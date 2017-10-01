namespace Shatulsky_Farm {
    partial class MainForm {
        /// <summary>
        /// Обязательная переменная конструктора.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Освободить все используемые ресурсы.
        /// </summary>
        /// <param name="disposing">истинно, если управляемый ресурс должен быть удален; иначе ложно.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Код, автоматически созданный конструктором форм Windows

        /// <summary>
        /// Требуемый метод для поддержки конструктора — не изменяйте 
        /// содержимое этого метода с помощью редактора кода.
        /// </summary>
        private void InitializeComponent() {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.ServersRichTextBox = new System.Windows.Forms.RichTextBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.GotoCatalogLable = new System.Windows.Forms.Label();
            this.GotoCatalogBox = new System.Windows.Forms.TextBox();
            this.KeysShopLable = new System.Windows.Forms.Label();
            this.KeysShopKey = new System.Windows.Forms.TextBox();
            this.ApikeyLable = new System.Windows.Forms.Label();
            this.ApikeyBox = new System.Windows.Forms.TextBox();
            this.ServersLabel = new System.Windows.Forms.Label();
            this.BotsLoadedCountLable = new System.Windows.Forms.Label();
            this.BotsLoadedLabel = new System.Windows.Forms.Label();
            this.BuyGamesButton = new System.Windows.Forms.Button();
            this.MafileFolderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
            this.LogBox = new System.Windows.Forms.RichTextBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.MaxMoneyLable = new System.Windows.Forms.Label();
            this.MaxMoneyBox = new System.Windows.Forms.TextBox();
            this.QiwiTokenLable = new System.Windows.Forms.Label();
            this.QiwiTokenBox = new System.Windows.Forms.TextBox();
            this.EmailLabel = new System.Windows.Forms.Label();
            this.EmailBox = new System.Windows.Forms.TextBox();
            this.MaxGameCostLable = new System.Windows.Forms.Label();
            this.MaxGameCostBox = new System.Windows.Forms.TextBox();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.WastedManeyCountLable = new System.Windows.Forms.Label();
            this.WastedManeyLable = new System.Windows.Forms.Label();
            this.ActivateUnusedKeysButton = new System.Windows.Forms.Button();
            this.ActivateKeysButton = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.SuspendLayout();
            // 
            // ServersRichTextBox
            // 
            this.ServersRichTextBox.Location = new System.Drawing.Point(6, 37);
            this.ServersRichTextBox.Name = "ServersRichTextBox";
            this.ServersRichTextBox.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.ForcedVertical;
            this.ServersRichTextBox.Size = new System.Drawing.Size(211, 131);
            this.ServersRichTextBox.TabIndex = 0;
            this.ServersRichTextBox.Text = "";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.GotoCatalogLable);
            this.groupBox1.Controls.Add(this.GotoCatalogBox);
            this.groupBox1.Controls.Add(this.KeysShopLable);
            this.groupBox1.Controls.Add(this.KeysShopKey);
            this.groupBox1.Controls.Add(this.ApikeyLable);
            this.groupBox1.Controls.Add(this.ApikeyBox);
            this.groupBox1.Controls.Add(this.ServersLabel);
            this.groupBox1.Controls.Add(this.ServersRichTextBox);
            this.groupBox1.Location = new System.Drawing.Point(12, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(226, 301);
            this.groupBox1.TabIndex = 1;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Дедики/Каталог";
            // 
            // GotoCatalogLable
            // 
            this.GotoCatalogLable.AutoSize = true;
            this.GotoCatalogLable.Location = new System.Drawing.Point(6, 256);
            this.GotoCatalogLable.Name = "GotoCatalogLable";
            this.GotoCatalogLable.Size = new System.Drawing.Size(100, 13);
            this.GotoCatalogLable.TabIndex = 8;
            this.GotoCatalogLable.Text = "Key из gotoCatalog";
            // 
            // GotoCatalogBox
            // 
            this.GotoCatalogBox.Location = new System.Drawing.Point(6, 272);
            this.GotoCatalogBox.Name = "GotoCatalogBox";
            this.GotoCatalogBox.PasswordChar = '*';
            this.GotoCatalogBox.Size = new System.Drawing.Size(211, 20);
            this.GotoCatalogBox.TabIndex = 7;
            // 
            // KeysShopLable
            // 
            this.KeysShopLable.AutoSize = true;
            this.KeysShopLable.Location = new System.Drawing.Point(6, 215);
            this.KeysShopLable.Name = "KeysShopLable";
            this.KeysShopLable.Size = new System.Drawing.Size(98, 13);
            this.KeysShopLable.TabIndex = 6;
            this.KeysShopLable.Text = "steamkeys.ovh API";
            // 
            // KeysShopKey
            // 
            this.KeysShopKey.Location = new System.Drawing.Point(6, 231);
            this.KeysShopKey.Name = "KeysShopKey";
            this.KeysShopKey.PasswordChar = '*';
            this.KeysShopKey.Size = new System.Drawing.Size(211, 20);
            this.KeysShopKey.TabIndex = 5;
            // 
            // ApikeyLable
            // 
            this.ApikeyLable.AutoSize = true;
            this.ApikeyLable.Location = new System.Drawing.Point(6, 174);
            this.ApikeyLable.Name = "ApikeyLable";
            this.ApikeyLable.Size = new System.Drawing.Size(64, 13);
            this.ApikeyLable.TabIndex = 4;
            this.ApikeyLable.Text = "STEAM API";
            // 
            // ApikeyBox
            // 
            this.ApikeyBox.Location = new System.Drawing.Point(6, 190);
            this.ApikeyBox.Name = "ApikeyBox";
            this.ApikeyBox.PasswordChar = '*';
            this.ApikeyBox.Size = new System.Drawing.Size(214, 20);
            this.ApikeyBox.TabIndex = 2;
            // 
            // ServersLabel
            // 
            this.ServersLabel.AutoSize = true;
            this.ServersLabel.Location = new System.Drawing.Point(3, 21);
            this.ServersLabel.Name = "ServersLabel";
            this.ServersLabel.Size = new System.Drawing.Size(45, 13);
            this.ServersLabel.TabIndex = 1;
            this.ServersLabel.Text = "IP:Порт";
            // 
            // BotsLoadedCountLable
            // 
            this.BotsLoadedCountLable.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.BotsLoadedCountLable.Location = new System.Drawing.Point(104, 14);
            this.BotsLoadedCountLable.Name = "BotsLoadedCountLable";
            this.BotsLoadedCountLable.Size = new System.Drawing.Size(67, 18);
            this.BotsLoadedCountLable.TabIndex = 3;
            this.BotsLoadedCountLable.Text = "0";
            this.BotsLoadedCountLable.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // BotsLoadedLabel
            // 
            this.BotsLoadedLabel.AutoSize = true;
            this.BotsLoadedLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.BotsLoadedLabel.Location = new System.Drawing.Point(7, 17);
            this.BotsLoadedLabel.Name = "BotsLoadedLabel";
            this.BotsLoadedLabel.Size = new System.Drawing.Size(100, 13);
            this.BotsLoadedLabel.TabIndex = 4;
            this.BotsLoadedLabel.Text = "Ботов загружено -";
            // 
            // BuyGamesButton
            // 
            this.BuyGamesButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.BuyGamesButton.ForeColor = System.Drawing.SystemColors.ControlText;
            this.BuyGamesButton.Location = new System.Drawing.Point(476, 246);
            this.BuyGamesButton.Name = "BuyGamesButton";
            this.BuyGamesButton.Size = new System.Drawing.Size(122, 81);
            this.BuyGamesButton.TabIndex = 2;
            this.BuyGamesButton.Text = "Покупать игры";
            this.BuyGamesButton.UseVisualStyleBackColor = true;
            this.BuyGamesButton.Click += new System.EventHandler(this.StartButton_Click);
            // 
            // LogBox
            // 
            this.LogBox.Location = new System.Drawing.Point(476, 12);
            this.LogBox.Name = "LogBox";
            this.LogBox.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.ForcedVertical;
            this.LogBox.Size = new System.Drawing.Size(252, 228);
            this.LogBox.TabIndex = 7;
            this.LogBox.Text = "";
            this.LogBox.TextChanged += new System.EventHandler(this.LogBox_TextChanged);
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.MaxMoneyLable);
            this.groupBox2.Controls.Add(this.MaxMoneyBox);
            this.groupBox2.Controls.Add(this.QiwiTokenLable);
            this.groupBox2.Controls.Add(this.QiwiTokenBox);
            this.groupBox2.Controls.Add(this.EmailLabel);
            this.groupBox2.Controls.Add(this.EmailBox);
            this.groupBox2.Controls.Add(this.MaxGameCostLable);
            this.groupBox2.Controls.Add(this.MaxGameCostBox);
            this.groupBox2.Location = new System.Drawing.Point(244, 12);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(226, 228);
            this.groupBox2.TabIndex = 7;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Настройки оплаты";
            // 
            // MaxMoneyLable
            // 
            this.MaxMoneyLable.AutoSize = true;
            this.MaxMoneyLable.Location = new System.Drawing.Point(6, 62);
            this.MaxMoneyLable.Name = "MaxMoneyLable";
            this.MaxMoneyLable.Size = new System.Drawing.Size(138, 13);
            this.MaxMoneyLable.TabIndex = 18;
            this.MaxMoneyLable.Text = "Максимум потратить, руб";
            // 
            // MaxMoneyBox
            // 
            this.MaxMoneyBox.Location = new System.Drawing.Point(6, 77);
            this.MaxMoneyBox.Name = "MaxMoneyBox";
            this.MaxMoneyBox.Size = new System.Drawing.Size(214, 20);
            this.MaxMoneyBox.TabIndex = 17;
            // 
            // QiwiTokenLable
            // 
            this.QiwiTokenLable.AutoSize = true;
            this.QiwiTokenLable.Location = new System.Drawing.Point(6, 142);
            this.QiwiTokenLable.Name = "QiwiTokenLable";
            this.QiwiTokenLable.Size = new System.Drawing.Size(65, 13);
            this.QiwiTokenLable.TabIndex = 16;
            this.QiwiTokenLable.Text = "Токен киви";
            // 
            // QiwiTokenBox
            // 
            this.QiwiTokenBox.Location = new System.Drawing.Point(6, 157);
            this.QiwiTokenBox.Name = "QiwiTokenBox";
            this.QiwiTokenBox.PasswordChar = '*';
            this.QiwiTokenBox.Size = new System.Drawing.Size(214, 20);
            this.QiwiTokenBox.TabIndex = 15;
            // 
            // EmailLabel
            // 
            this.EmailLabel.AutoSize = true;
            this.EmailLabel.Location = new System.Drawing.Point(6, 100);
            this.EmailLabel.Name = "EmailLabel";
            this.EmailLabel.Size = new System.Drawing.Size(32, 13);
            this.EmailLabel.TabIndex = 12;
            this.EmailLabel.Text = "Email";
            // 
            // EmailBox
            // 
            this.EmailBox.Location = new System.Drawing.Point(6, 115);
            this.EmailBox.Name = "EmailBox";
            this.EmailBox.Size = new System.Drawing.Size(214, 20);
            this.EmailBox.TabIndex = 11;
            // 
            // MaxGameCostLable
            // 
            this.MaxGameCostLable.AutoSize = true;
            this.MaxGameCostLable.Location = new System.Drawing.Point(6, 22);
            this.MaxGameCostLable.Name = "MaxGameCostLable";
            this.MaxGameCostLable.Size = new System.Drawing.Size(203, 13);
            this.MaxGameCostLable.TabIndex = 10;
            this.MaxGameCostLable.Text = "Макс. стоимость игры (включительно)";
            // 
            // MaxGameCostBox
            // 
            this.MaxGameCostBox.Location = new System.Drawing.Point(6, 37);
            this.MaxGameCostBox.Name = "MaxGameCostBox";
            this.MaxGameCostBox.Size = new System.Drawing.Size(214, 20);
            this.MaxGameCostBox.TabIndex = 9;
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.WastedManeyCountLable);
            this.groupBox3.Controls.Add(this.WastedManeyLable);
            this.groupBox3.Controls.Add(this.BotsLoadedCountLable);
            this.groupBox3.Controls.Add(this.BotsLoadedLabel);
            this.groupBox3.Location = new System.Drawing.Point(244, 246);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(226, 67);
            this.groupBox3.TabIndex = 17;
            this.groupBox3.TabStop = false;
            // 
            // WastedManeyCountLable
            // 
            this.WastedManeyCountLable.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.WastedManeyCountLable.Location = new System.Drawing.Point(104, 39);
            this.WastedManeyCountLable.Name = "WastedManeyCountLable";
            this.WastedManeyCountLable.Size = new System.Drawing.Size(67, 18);
            this.WastedManeyCountLable.TabIndex = 5;
            this.WastedManeyCountLable.Text = "0";
            this.WastedManeyCountLable.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // WastedManeyLable
            // 
            this.WastedManeyLable.AutoSize = true;
            this.WastedManeyLable.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.WastedManeyLable.Location = new System.Drawing.Point(7, 42);
            this.WastedManeyLable.Name = "WastedManeyLable";
            this.WastedManeyLable.Size = new System.Drawing.Size(101, 13);
            this.WastedManeyLable.TabIndex = 6;
            this.WastedManeyLable.Text = "Всего потрачено - ";
            // 
            // ActivateUnusedKeysButton
            // 
            this.ActivateUnusedKeysButton.Location = new System.Drawing.Point(606, 288);
            this.ActivateUnusedKeysButton.Name = "ActivateUnusedKeysButton";
            this.ActivateUnusedKeysButton.Size = new System.Drawing.Size(122, 39);
            this.ActivateUnusedKeysButton.TabIndex = 18;
            this.ActivateUnusedKeysButton.Text = "Активировать UNUSEDKEYS.TXT";
            this.ActivateUnusedKeysButton.UseVisualStyleBackColor = true;
            this.ActivateUnusedKeysButton.Click += new System.EventHandler(this.LootButton_Click);
            // 
            // ActivateKeysButton
            // 
            this.ActivateKeysButton.Location = new System.Drawing.Point(606, 246);
            this.ActivateKeysButton.Name = "ActivateKeysButton";
            this.ActivateKeysButton.Size = new System.Drawing.Size(122, 39);
            this.ActivateKeysButton.TabIndex = 19;
            this.ActivateKeysButton.Text = "Активировать ключи из /activate/";
            this.ActivateKeysButton.UseVisualStyleBackColor = true;
            this.ActivateKeysButton.Click += new System.EventHandler(this.ActivateKeysButton_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(737, 330);
            this.Controls.Add(this.ActivateKeysButton);
            this.Controls.Add(this.ActivateUnusedKeysButton);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.LogBox);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.BuyGamesButton);
            this.Controls.Add(this.groupBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.Text = "Shatulsky Farm";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        public System.Windows.Forms.RichTextBox ServersRichTextBox;
        public System.Windows.Forms.GroupBox groupBox1;
        public System.Windows.Forms.Label ServersLabel;
        public System.Windows.Forms.Button BuyGamesButton;
        public System.Windows.Forms.TextBox ApikeyBox;
        public System.Windows.Forms.FolderBrowserDialog MafileFolderBrowserDialog;
        public System.Windows.Forms.Label ApikeyLable;
        public System.Windows.Forms.Label KeysShopLable;
        public System.Windows.Forms.TextBox KeysShopKey;
        public System.Windows.Forms.Label BotsLoadedLabel;
        public System.Windows.Forms.Label BotsLoadedCountLable;
        public System.Windows.Forms.RichTextBox LogBox;
        public System.Windows.Forms.GroupBox groupBox2;
        public System.Windows.Forms.Label GotoCatalogLable;
        public System.Windows.Forms.TextBox GotoCatalogBox;
        public System.Windows.Forms.Label MaxGameCostLable;
        public System.Windows.Forms.TextBox MaxGameCostBox;
        public System.Windows.Forms.Label EmailLabel;
        public System.Windows.Forms.TextBox EmailBox;
        public System.Windows.Forms.Label QiwiTokenLable;
        public System.Windows.Forms.TextBox QiwiTokenBox;
        private System.Windows.Forms.GroupBox groupBox3;
        public System.Windows.Forms.Label MaxMoneyLable;
        public System.Windows.Forms.TextBox MaxMoneyBox;
        public System.Windows.Forms.Label WastedManeyCountLable;
        public System.Windows.Forms.Label WastedManeyLable;
        public System.Windows.Forms.Button ActivateUnusedKeysButton;
        public System.Windows.Forms.Button ActivateKeysButton;
    }
}

