using System;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;
using System.Collections.Generic;

namespace QueueFightGame.UI
{
    public partial class GameSetupForm : Form
    {
        private Button randomButton;
        private Button purchaseButton;
        private Button backButton;
        private ILogger uiLogger;

        private const string BgPath = "Resources/zastavka.png";
        private readonly Color BtnBack = Color.FromArgb(40, 40, 40);
        private readonly Color BtnBorder = Color.FromArgb(80, 0, 0);
        private readonly Color BtnHover = Color.FromArgb(70, 70, 70);

        private const float DefaultBudget = 100;

        public GameSetupForm()
        {
            InitializeComponent();
            SetupCustomComponents();
            uiLogger = new MemoryLogger();
        }

        private void SetupCustomComponents()
        {
            Text = "Настройка Игры";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(400, 300);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;

            TrySetBackground(BgPath);

            randomButton = CreateButton("Случайный Бой", 140, 50, RandomButton_Click);
            purchaseButton = CreateButton("Собрать Команды", 140, 110, PurchaseButton_Click);
            backButton = CreateButton("Назад", 140, 170, BackButton_Click);

            Controls.AddRange(new Control[] { randomButton, purchaseButton, backButton });
        }

        private Button CreateButton(string text, int x, int y, EventHandler onClick)
        {
            var btn = new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(120, 40),
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.Gainsboro,
                BackColor = BtnBack,
                FlatStyle = FlatStyle.Flat
            };
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = BtnBorder;
            btn.FlatAppearance.MouseDownBackColor = BtnBorder;
            btn.FlatAppearance.MouseOverBackColor = BtnHover;
            btn.Click += onClick;
            return btn;
        }

        private void TrySetBackground(string path)
        {
            try
            {
                BackgroundImage = Image.FromFile(path);
                BackgroundImageLayout = ImageLayout.Stretch;
            }
            catch { }
        }

        private void RandomButton_Click(object sender, EventArgs e)
        {
            var redTeam = GenerateRandomTeam("Красные", DefaultBudget);
            var blueTeam = GenerateRandomTeam("Синие", DefaultBudget);

            if (!IsTeamValid(redTeam) || !IsTeamValid(blueTeam))
            {
                MessageBox.Show("Не удалось сгенерировать случайные команды. Возможно, недостаточно бюджета или нет доступных юнитов.",
                    "Ошибка генерации", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            StartBattle(redTeam, blueTeam);
        }

        private Team GenerateRandomTeam(string name, float budget)
        {
            var team = new Team(name, budget);
            var availableTypes = UnitConfig.Stats.Keys.ToList();
            var rnd = new Random();
            int attempts = 0;

            while (team.CurrentMoney > 0 && availableTypes.Any() && attempts < 50)
            {
                string type = availableTypes[rnd.Next(availableTypes.Count)];
                var data = UnitConfig.Stats[type];

                if (team.CanAfford(data.Cost))
                {
                    var unit = UnitFactory.CreateUnit(type);
                    team.AddFighter(unit, null);
                }
                else
                {
                    availableTypes.Remove(type);
                }
                attempts++;
            }

            if (!team.Fighters.Any())
            {
                var cheapest = UnitConfig.Stats.OrderBy(kv => kv.Value.Cost).FirstOrDefault();
                if (cheapest.Value != null && team.CanAfford(cheapest.Value.Cost))
                {
                    var unit = UnitFactory.CreateUnit(cheapest.Key);
                    team.AddFighter(unit, null);
                }
            }
            return team;
        }

        private bool IsTeamValid(Team team)
        {
            return team != null && team.HasFighters();
        }

        private void PurchaseButton_Click(object sender, EventArgs e)
        {
            var purchaseForm = new TeamPurchaseForm(DefaultBudget);
            purchaseForm.Show();
            Hide();
            purchaseForm.FormClosed += (s, args) => Close();
        }

        private void BackButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void StartBattle(Team redTeam, Team blueTeam)
        {
            var battleForm = new BattleForm(redTeam, blueTeam);
            battleForm.Show();
            Hide();
            battleForm.FormClosed += (s, args) => Close();
        }

        private void InitializeComponent()
        {
            SuspendLayout();
            Name = "GameSetupForm";
            ResumeLayout(false);
        }
    }
}
