﻿using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using Khronos_PMS.Util;
using Khronos_PMS.Model;

namespace Khronos_PMS {
    public partial class LoginForm : Form {
        private User user;

        public LoginForm() {
            InitializeComponent();
        }

        private void passwordTextBox_PreviewKeyDown(Object sender, PreviewKeyDownEventArgs e) {
            if (e.KeyCode == Keys.Enter)
                login();
        }

        private void loginButton_Click(Object sender, EventArgs e) {
            login();
        }

        private async void login() {
            if (errorTextLabel.Visible)
                errorTextLabel.Visible = false;

            String username = usernameTextBox.Text;
            String password = passwordTextBox.Text;

            if (String.IsNullOrEmpty(username) || String.IsNullOrEmpty(password)) {
                if (String.IsNullOrEmpty(username))
                    usernameTextBox.CreateGraphics().DrawRectangle(Pens.Red, 0, 0, usernameTextBox.Width - 1, usernameTextBox.Height - 1);
                if (String.IsNullOrEmpty(password))
                    passwordTextBox.CreateGraphics().DrawRectangle(Pens.Red, 0, 0, passwordTextBox.Width - 1, passwordTextBox.Height - 1);
            } else {
                LoginManager login = null;
                progressBar.Visible = true;
                await Task.Run(() => { login = new LoginManager(username, password); });
                if (login.IsValid) {
                    DialogResult = DialogResult.OK;
                    user = login.User;
                } else {
                    errorTextLabel.Text = login.Message;
                    errorTextLabel.Visible = true;
                }
                progressBar.Visible = false;
            }
        }

        private void cancelButton_Click(Object sender, EventArgs e) {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        public User User {
            get { return user; }
        }
    }
}