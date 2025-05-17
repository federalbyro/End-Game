using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace QueueFightGame.UI
{
    public partial class MainMenuForm : Form
    {
        private Button playButton;

        public MainMenuForm()
        {
            InitializeComponent();
            SetupCustomComponents();
        }

        private void SetupCustomComponents()
        {
            this.Text = "Queue Fight Game - Главное Меню";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.ClientSize = new Size(800, 600);

            BackgroundImage = Image.FromFile("Resources/zastavka.png");
            BackgroundImageLayout = ImageLayout.Stretch;
            try
            {
                this.BackgroundImageLayout = ImageLayout.Stretch;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось загрузить фон: {ex.Message}");
            }

            var loadButton = new Button
            {
                Text = "Загрузить игру",
                Size = new Size(180, 60),
                Location = new Point((ClientSize.Width - 180) / 2, (ClientSize.Height - 60) / 2 + 100),
                Font = new Font("Segoe UI", 12f, FontStyle.Bold)
            };
            loadButton.Click += (s, e) => {
                using (var dlg = new OpenFileDialog { Filter = "JSON|*.json" })
                    if (dlg.ShowDialog() == DialogResult.OK)
                    {
                        var mgr = new GameManager(new MemoryLogger());
                        mgr.LoadState(dlg.FileName);
                        var bf = new BattleForm(mgr);
                        bf.Show();
                        this.Hide();
                        bf.FormClosed += (s2, e2) => this.Close();


                    }
            };

            this.Controls.Add(loadButton);


            playButton = new Button
            {
                Size = new Size(200, 80),
                BackgroundImage = Image.FromFile("Resources/knopkaplay.png"),
                BackgroundImageLayout = ImageLayout.Stretch,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                FlatAppearance =
        {
            BorderSize   = 0,
            MouseDownBackColor = Color.Transparent,
            MouseOverBackColor = Color.Transparent
        }
            };
            playButton.Location = new Point((ClientSize.Width - playButton.Width) / 2,
                                             (ClientSize.Height - playButton.Height) / 2);

            playButton.Click += PlayButton_Click;

            this.Controls.Add(playButton);
        }

        private void PlayButton_Click(object sender, EventArgs e)
        {
            GameSetupForm setupForm = new GameSetupForm();
            setupForm.Show();
            this.Hide();

            setupForm.FormClosed += (s, args) => this.Close();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            
            this.Name = "MainMenuForm";
            this.ResumeLayout(false);
        }
    }
}