using System;
using System.ComponentModel;
using System.Windows.Forms;
using mRemoteNG.App;
using mRemoteNG.App.Update;
using mRemoteNG.Security.SymmetricEncryption;
using mRemoteNG.Tools;
using mRemoteNG.UI.TaskDialog;
using mRemoteNG.UI.Window;

namespace mRemoteNG.UI.Forms.OptionsPages
{
    public partial class UpdatesPage
    {
        #region Private Fields

        private AppUpdater _appUpdate;

        #endregion

        public UpdatesPage()
        {
            InitializeComponent();
        }

        #region Public Methods

        public override string PageName
        {
            get { return Language.strTabUpdates; }
            set { }
        }

        public override void ApplyLanguage()
        {
            base.ApplyLanguage();

#if !PORTABLE
			lblUpdatesExplanation.Text = Language.strUpdateCheck;
            #else
            lblUpdatesExplanation.Text = Language.strUpdateCheckPortableEdition;
#endif

            chkCheckForUpdatesOnStartup.Text = Language.strCheckForUpdatesOnStartup;
            btnUpdateCheckNow.Text = Language.strCheckNow;

            chkUseProxyForAutomaticUpdates.Text = Language.strCheckboxUpdateUseProxy;
            lblProxyAddress.Text = Language.strLabelAddress;
            lblProxyPort.Text = Language.strLabelPort;

            chkUseProxyAuthentication.Text = Language.strCheckboxProxyAuthentication;
            lblProxyUsername.Text = Language.strLabelUsername;
            lblProxyPassword.Text = Language.strLabelPassword;

            btnTestProxy.Text = Language.strButtonTestProxy;
        }

        public override void LoadSettings()
        {
            base.SaveSettings();

            chkCheckForUpdatesOnStartup.Checked = Convert.ToBoolean(Settings.Default.CheckForUpdatesOnStartup);
            cboUpdateCheckFrequency.Enabled = chkCheckForUpdatesOnStartup.Checked;
            cboUpdateCheckFrequency.Items.Clear();
            var nDaily = cboUpdateCheckFrequency.Items.Add(Language.strUpdateFrequencyDaily);
            var nWeekly = cboUpdateCheckFrequency.Items.Add(Language.strUpdateFrequencyWeekly);
            var nMonthly = cboUpdateCheckFrequency.Items.Add(Language.strUpdateFrequencyMonthly);
            if (Settings.Default.CheckForUpdatesFrequencyDays < 1)
            {
                chkCheckForUpdatesOnStartup.Checked = false;
                cboUpdateCheckFrequency.SelectedIndex = nDaily;
            } // Daily
            else if (Settings.Default.CheckForUpdatesFrequencyDays == 1)
            {
                cboUpdateCheckFrequency.SelectedIndex = nDaily;
            } // Weekly
            else if (Settings.Default.CheckForUpdatesFrequencyDays == 7)
            {
                cboUpdateCheckFrequency.SelectedIndex = nWeekly;
            } // Monthly
            else if (Settings.Default.CheckForUpdatesFrequencyDays == 31)
            {
                cboUpdateCheckFrequency.SelectedIndex = nMonthly;
            }
            else
            {
                var nCustom =
                    cboUpdateCheckFrequency.Items.Add(string.Format(Language.strUpdateFrequencyCustom,
                        Settings.Default.CheckForUpdatesFrequencyDays));
                cboUpdateCheckFrequency.SelectedIndex = nCustom;
            }

            chkUseProxyForAutomaticUpdates.Checked = Convert.ToBoolean(Settings.Default.UpdateUseProxy);
            pnlProxyBasic.Enabled = Convert.ToBoolean(Settings.Default.UpdateUseProxy);
            txtProxyAddress.Text = Convert.ToString(Settings.Default.UpdateProxyAddress);
            numProxyPort.Value = Convert.ToDecimal(Settings.Default.UpdateProxyPort);

            chkUseProxyAuthentication.Checked = Convert.ToBoolean(Settings.Default.UpdateProxyUseAuthentication);
            pnlProxyAuthentication.Enabled = Convert.ToBoolean(Settings.Default.UpdateProxyUseAuthentication);
            txtProxyUsername.Text = Convert.ToString(Settings.Default.UpdateProxyAuthUser);
            var cryptographyProvider = new LegacyRijndaelCryptographyProvider();
            txtProxyPassword.Text = cryptographyProvider.Decrypt(Convert.ToString(Settings.Default.UpdateProxyAuthPass), Runtime.EncryptionKey);

            btnTestProxy.Enabled = Convert.ToBoolean(Settings.Default.UpdateUseProxy);

#if PORTABLE
            foreach (Control Control in Controls)
            {
                if (Control != lblUpdatesExplanation)
                {
                    Control.Visible = false;
                }
            }
#endif
        }

        public override void SaveSettings()
        {
            base.SaveSettings();

            Settings.Default.CheckForUpdatesOnStartup = chkCheckForUpdatesOnStartup.Checked;
            if (cboUpdateCheckFrequency.SelectedItem.ToString() == Language.strUpdateFrequencyDaily)
            {
                Settings.Default.CheckForUpdatesFrequencyDays = 1;
            }
            else if (cboUpdateCheckFrequency.SelectedItem.ToString() == Language.strUpdateFrequencyWeekly)
            {
                Settings.Default.CheckForUpdatesFrequencyDays = 7;
            }
            else if (cboUpdateCheckFrequency.SelectedItem.ToString() == Language.strUpdateFrequencyMonthly)
            {
                Settings.Default.CheckForUpdatesFrequencyDays = 31;
            }

            Settings.Default.UpdateUseProxy = chkUseProxyForAutomaticUpdates.Checked;
            Settings.Default.UpdateProxyAddress = txtProxyAddress.Text;
            Settings.Default.UpdateProxyPort = (int) numProxyPort.Value;

            Settings.Default.UpdateProxyUseAuthentication = chkUseProxyAuthentication.Checked;
            Settings.Default.UpdateProxyAuthUser = txtProxyUsername.Text;
            var cryptographyProvider = new LegacyRijndaelCryptographyProvider();
            Settings.Default.UpdateProxyAuthPass = cryptographyProvider.Encrypt(txtProxyPassword.Text, Runtime.EncryptionKey);

            Settings.Default.Save();
        }

        #endregion

        #region Private Methods

        #region Event Handlers

        private void chkCheckForUpdatesOnStartup_CheckedChanged(object sender, EventArgs e)
        {
            cboUpdateCheckFrequency.Enabled = chkCheckForUpdatesOnStartup.Checked;
        }

        private void btnUpdateCheckNow_Click(object sender, EventArgs e)
        {
            Windows.Show(WindowType.Update);
        }

        private void chkUseProxyForAutomaticUpdates_CheckedChanged(object sender, EventArgs e)
        {
            pnlProxyBasic.Enabled = chkUseProxyForAutomaticUpdates.Checked;
            btnTestProxy.Enabled = chkUseProxyForAutomaticUpdates.Checked;

            if (chkUseProxyForAutomaticUpdates.Checked)
            {
                chkUseProxyAuthentication.Enabled = true;

                if (chkUseProxyAuthentication.Checked)
                {
                    pnlProxyAuthentication.Enabled = true;
                }
            }
            else
            {
                chkUseProxyAuthentication.Enabled = false;
                pnlProxyAuthentication.Enabled = false;
            }
        }

        private void btnTestProxy_Click(object sender, EventArgs e)
        {
            if (_appUpdate != null)
            {
                if (_appUpdate.IsGetUpdateInfoRunning)
                {
                    return;
                }
            }

            _appUpdate = new AppUpdater();
            //_appUpdate.Load += _appUpdate.Update_Load;
            _appUpdate.SetProxySettings(chkUseProxyForAutomaticUpdates.Checked, txtProxyAddress.Text,
                (int) numProxyPort.Value, chkUseProxyAuthentication.Checked, txtProxyUsername.Text,
                txtProxyPassword.Text);

            btnTestProxy.Enabled = false;
            btnTestProxy.Text = Language.strOptionsProxyTesting;

            _appUpdate.GetUpdateInfoCompletedEvent += GetUpdateInfoCompleted;

            _appUpdate.GetUpdateInfoAsync();
        }

        private void chkUseProxyAuthentication_CheckedChanged(object sender, EventArgs e)
        {
            if (chkUseProxyForAutomaticUpdates.Checked)
            {
                if (chkUseProxyAuthentication.Checked)
                {
                    pnlProxyAuthentication.Enabled = true;
                }
                else
                {
                    pnlProxyAuthentication.Enabled = false;
                }
            }
        }

        #endregion

        private void GetUpdateInfoCompleted(object sender, AsyncCompletedEventArgs e)
        {
            if (InvokeRequired)
            {
                AsyncCompletedEventHandler myDelegate = GetUpdateInfoCompleted;
                Invoke(myDelegate, sender, e);
                return;
            }

            try
            {
                _appUpdate.GetUpdateInfoCompletedEvent -= GetUpdateInfoCompleted;

                btnTestProxy.Enabled = true;
                btnTestProxy.Text = Language.strButtonTestProxy;

                if (e.Cancelled)
                {
                    return;
                }
                if (e.Error != null)
                {
                    throw e.Error;
                }

                CTaskDialog.ShowCommandBox(this, Convert.ToString(Application.ProductName),
                    Language.strProxyTestSucceeded, "", Language.strButtonOK, false);
            }
            catch (Exception ex)
            {
                CTaskDialog.ShowCommandBox(this, Convert.ToString(Application.ProductName), Language.strProxyTestFailed,
                    MiscTools.GetExceptionMessageRecursive(ex), "", "", "", Language.strButtonOK, false, ESysIcons.Error,
                    0);
            }
        }

        #endregion
    }
}