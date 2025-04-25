using System.Drawing;
using System.Windows.Forms;

namespace QueueFightersGameWinForm
{
    public class FighterCard : Panel
    {
        public string Type { get; }
        public int Count { get; private set; }
        public float Cost { get; }
        public Button BuyButton { get; }
        public Button RemoveButton { get; }
        public Label CountLabel { get; }

        public FighterCard(string type, string name, string description, float cost, Image image)
        {
            Type = type;
            Cost = cost;
            Count = 0;

            // Настройка карточки
            Size = new Size(220, 350);
            BackColor = Color.FromArgb(60, 60, 65);
            BorderStyle = BorderStyle.FixedSingle;
            Margin = new Padding(10);

            // Заголовок карточки
            var nameLabel = new Label
            {
                Text = name,
                Font = new Font("Arial", 14, FontStyle.Bold),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(Width - 20, 30),
                Location = new Point(10, 10)
            };

            // Стоимость бойца
            var costLabel = new Label
            {
                Text = $"Стоимость: {cost}",
                Font = new Font("Arial", 12),
                ForeColor = Color.Gold,
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(Width - 20, 30),
                Location = new Point(10, 40)
            };

            // Изображение бойца
            var imageBox = new PictureBox
            {
                Size = new Size(120, 120),
                Location = new Point((Width - 120) / 2, 80),
                Image = image,
                SizeMode = PictureBoxSizeMode.Zoom
            };

            // Описание бойца
            var descLabel = new Label
            {
                Text = description,
                Font = new Font("Arial", 10),
                ForeColor = Color.LightGray,
                TextAlign = ContentAlignment.TopLeft,
                Size = new Size(Width - 20, 60),
                Location = new Point(10, 210)
            };

            // Счетчик количества
            CountLabel = new Label
            {
                Text = "0",
                Font = new Font("Arial", 16, FontStyle.Bold),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(50, 30),
                Location = new Point((Width - 50) / 2, 280)
            };

            // Кнопка добавления
            BuyButton = new Button
            {
                Text = "+",
                Font = new Font("Arial", 14, FontStyle.Bold),
                Size = new Size(40, 40),
                Location = new Point(Width - 50, 275),
                BackColor = Color.FromArgb(86, 156, 86),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Tag = this
            };
            BuyButton.FlatAppearance.BorderSize = 0;

            // Кнопка удаления
            RemoveButton = new Button
            {
                Text = "-",
                Font = new Font("Arial", 14, FontStyle.Bold),
                Size = new Size(40, 40),
                Location = new Point(10, 275),
                BackColor = Color.FromArgb(156, 86, 86),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Tag = this
            };
            RemoveButton.FlatAppearance.BorderSize = 0;

            Controls.AddRange(new Control[] { nameLabel, costLabel, imageBox, descLabel, CountLabel, BuyButton, RemoveButton });
        }

        public void UpdateCount(int newCount)
        {
            Count = newCount;
            CountLabel.Text = Count.ToString();
        }
    }
}