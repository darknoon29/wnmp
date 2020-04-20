﻿/*
 * Copyright (c) 2012 - 2017, Kurt Cancemi (kurt@x64architecture.com)
 *
 * This file is part of Wnmp.
 *
 *  Wnmp is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  Wnmp is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with Wnmp.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Windows.Forms;
using Wnmp.Properties;
using Wnmp.UI;

namespace Wnmp.Wnmp.UI
{
    public partial class AboutFrm : Form
    {
        public AboutFrm()
        {
            InitializeComponent();
        }

        private void AboutFrm_Load(object sender, EventArgs e)
        {
            wnmpversionLabel.Text = Resources.AboutFrm_Wnmp_Version__ + Application.ProductVersion;
        }

        private void CloseButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void wnmpWebsiteLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Misc.StartProcessAsync("https://wnmp.x64architecture.com");
        }
    }
}
