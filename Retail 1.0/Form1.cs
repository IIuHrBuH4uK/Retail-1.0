using Microsoft.EntityFrameworkCore.Query.Internal;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Windows.Forms;

namespace Retail_1._0
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            SqlConnection sqlConnection = new SqlConnection(@"Data Source=HOME-PC;Initial Catalog=Apteka;Integrated Security=True;Connect Timeout=30;Encrypt=True;TrustServerCertificate=True;ApplicationIntent=ReadWrite;MultiSubnetFailover=False");
            sqlConnection.Open();

            SqlCommand command = new SqlCommand("SELECT * FROM Table_1", sqlConnection);
            SqlDataReader reader = command.ExecuteReader();

            List<string[]> data = new List<string[]>();

            while (reader.Read())
            {
                int fieldCount = reader.FieldCount;
                string[] row = new string[fieldCount];

                for (int i = 0; i < fieldCount; i++)
                {
                    row[i] = reader.IsDBNull(i) ? string.Empty : reader[i].ToString();
                }

                // Найти индекс столбца Value
                int valueIndex = reader.GetOrdinal("Value");
                int quantity = int.Parse(row[valueIndex]);

                // Добавляем строку в BaseGrid только если количество больше 0
                if (quantity > 0)
                {
                    data.Add(row);
                }
            }

            reader.Close();
            sqlConnection.Close();

            foreach (string[] s in data)
            {
                BaseGrid.Rows.Add(s);
            }
        }

        private void MoveSelectedItem()
        {
            if (BaseGrid.SelectedRows.Count > 0)
            {
                DataGridViewRow selectedRow = BaseGrid.SelectedRows[0];
                int productId = int.Parse(selectedRow.Cells["ProductId"].Value.ToString()); // Получаем идентификатор продукта
                int currentQuantity = int.Parse(selectedRow.Cells["ValueBase"].Value.ToString());
                string productName = selectedRow.Cells["NameBase"].Value.ToString();
                decimal price = decimal.Parse(selectedRow.Cells["Price"].Value.ToString());


                // Проверяем, существует ли строка с таким же именем в SellGrid
                DataGridViewRow existingRow = null;
                foreach (DataGridViewRow row in SellGrid.Rows)
                {
                    if (row.Cells["NameSell"].Value.ToString() == productName)
                    {
                        existingRow = row;
                        break;
                    }
                }

                if (existingRow != null)
                {
                    // Если строка существует, увеличиваем количество
                    int existingQuantity = int.Parse(existingRow.Cells["ValueSell"].Value.ToString());
                    existingRow.Cells["ValueSell"].Value = existingQuantity + 1;
                    existingRow.Cells["SummNDSSell"].Value = (existingQuantity + 1) * price;

                }
                else
                {
                    // Если строка не существует, добавляем новую строку
                    DataGridViewRow newRow = new DataGridViewRow();
                    newRow.CreateCells(SellGrid);

                    newRow.Cells[SellGrid.Columns["NameSell"].Index].Value = productName; // Добавляем значение NameBase в NameSell
                    newRow.Cells[SellGrid.Columns["ValueSell"].Index].Value = 1; // Добавляем значение Value в ValueSell
                    newRow.Cells[SellGrid.Columns["PriceNDSSell"].Index].Value = price; // Добавляем значение Price в PriceNDSSell
                    newRow.Cells[SellGrid.Columns["SummNDSSell"].Index].Value = price; // Добавляем сумма в PriceNDSSell
                    newRow.Cells[SellGrid.Columns["BonusSell"].Index].Value = selectedRow.Cells["BonusBase"].Value;
                    newRow.Cells[SellGrid.Columns["SellId"].Index].Value = selectedRow.Cells["ProductId"].Value;



                    SellGrid.Rows.Add(newRow);
                }
                // Уменьшаем количество в базе данных
                UpdateProductQuantity(productId, -1);

                if (currentQuantity > 1)
                {
                    // Уменьшаем количество в BaseGrid на 1
                    selectedRow.Cells["ValueBase"].Value = currentQuantity - 1;
                }
                else
                {
                    // Удаляем строку из BaseGrid, если количество равно 1
                    BaseGrid.Rows.Remove(selectedRow);
                }
                label10.Enabled = Enabled;
                label11.Enabled = Enabled;
                label12.Enabled = Enabled;
                UpdateBonusSum();
                UpdateTotalSum();
            }
        }

        private void DeletSellGrid()
        {

            if (SellGrid.SelectedRows.Count > 0)
            {
                DataGridViewRow selectedRow = SellGrid.SelectedRows[0];
                string productName = selectedRow.Cells["NameSell"].Value.ToString();
                int sellQuantity = int.Parse(selectedRow.Cells["ValueSell"].Value.ToString());
                foreach (DataGridViewRow row in BaseGrid.Rows)
                {
                    if (row.Cells["NameBase"].Value != null && row.Cells["NameBase"].Value.ToString() == productName)
                    {
                        int baseQuantity = int.Parse(row.Cells["ValueBase"].Value.ToString());
                        row.Cells["ValueBase"].Value = baseQuantity + 1;

                        if (sellQuantity > 1)
                        {
                            // Уменьшаем количество в SellGrid на 1
                            selectedRow.Cells["ValueSell"].Value = sellQuantity - 1;
                        }
                        else
                        {
                            // Удаляем строку из BaseGrid, если количество равно 1
                            SellGrid.Rows.Remove(selectedRow);
                        };
                        UpdateProductQuantity(row.Cells["ProductId"].Value.ToString(), sellQuantity); // Обновить количество в базе данных
                        break;
                    }

                }
            }
        }

        private void CheckSellGridRows()
        {
            if (SellGrid.Rows.Count == 0)
            {
                label4.Text = "0,00₽";
            }
        }

        private void ClearSellGrid()
        {
            foreach (DataGridViewRow sellRow in SellGrid.Rows)
            {
                string productName = sellRow.Cells["NameSell"].Value.ToString();
                int sellQuantity = int.Parse(sellRow.Cells["ValueSell"].Value.ToString());
                foreach (DataGridViewRow baseRow in BaseGrid.Rows)
                {
                    if (baseRow.Cells["NameBase"].Value != null && baseRow.Cells["NameBase"].Value.ToString() == productName)
                    {
                        int baseQuantity = int.Parse(baseRow.Cells["ValueBase"].Value.ToString());
                        baseRow.Cells["ValueBase"].Value = baseQuantity + sellQuantity;
                        UpdateProductQuantity(baseRow.Cells["ProductId"].Value.ToString(), sellQuantity); // Обновить количество в базе данных
                        break;
                    }
                }
            }
            SellGrid.Rows.Clear();
            label4.Text = "0,00₽";
            label8.Text = "0,00₽";
            label16.Text = "0,00₽";


        }


        private void UpdateProductQuantity(string productId, int quantityChange)
        {
            using (SqlConnection sqlConnection = new SqlConnection(@"Data Source=HOME-PC;Initial Catalog=Apteka;Integrated Security=True;Connect Timeout=30;Encrypt=True;TrustServerCertificate=True;ApplicationIntent=ReadWrite;MultiSubnetFailover=False"))
            {
                sqlConnection.Open();

                using (SqlCommand command = new SqlCommand("UPDATE Table_1 SET Value = Value + @Value WHERE ProductId = @ProductId", sqlConnection))
                {
                    command.Parameters.AddWithValue("@Value", quantityChange);
                    command.Parameters.AddWithValue("@ProductId", productId);

                    command.ExecuteNonQuery();
                }
            }
        }

        private void UpdateBonusSum()
        {
            decimal bonusSum = 0;

            foreach (DataGridViewRow row in SellGrid.Rows)
            {
                if (decimal.TryParse(row.Cells["ValueSell"].Value.ToString(), out decimal valueSell) &&
                    decimal.TryParse(row.Cells["BonusSell"].Value.ToString(), out decimal bonus))
                {
                    bonusSum += valueSell * bonus;
                }
            }

            label8.Text = $"{bonusSum},00₽";
            label16.Text = $"{bonusSum},00₽";
        }

        private void UpdateTotalSum()
        {
            decimal totalSum = 0;

            foreach (DataGridViewRow row in SellGrid.Rows)
            {
                if (row.Cells["ValueSell"].Value != null && row.Cells["PriceNDSSell"].Value != null)
                {
                    if (decimal.TryParse(row.Cells["ValueSell"].Value.ToString(), out decimal valueSell) &&
                        decimal.TryParse(row.Cells["PriceNDSSell"].Value.ToString(), out decimal priceSell))
                    {
                        totalSum += valueSell * priceSell;
                    }
                }
            }

            label4.Text = $"{totalSum}₽";
        }


        private void UpdateProductQuantity(int productId, int quantityChange)
        {
            SqlConnection sqlConnection = new SqlConnection(@"Data Source=HOME-PC;Initial Catalog=Apteka;Integrated Security=True;Connect Timeout=30;Encrypt=True;TrustServerCertificate=True;ApplicationIntent=ReadWrite;MultiSubnetFailover=False");
            sqlConnection.Open();

            SqlCommand command = new SqlCommand("UPDATE Table_1 SET Value = Value + @Value WHERE ProductId = @ProductId", sqlConnection);
            command.Parameters.AddWithValue("@Value", quantityChange);
            command.Parameters.AddWithValue("@ProductId", productId);

            command.ExecuteNonQuery();
            sqlConnection.Close();
        }



        private void BaseGrid_DoubleClick_1(object sender, EventArgs e)
        {
            MoveSelectedItem();
        }

        private void BaseGrid_KeyDown_1(object sender, KeyEventArgs e)
        {
            if (IsLetterKey(e.KeyCode))
            {
                char pressedKey = GetCharFromKey(e.KeyCode);
                ShowInputDialog(pressedKey);
            }

            // Проверяем нажатие Enter для перемещения элемента
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true; // предотвращаем стандартное поведение Enter
                MoveSelectedItem();
            }

            //// Предотвращаем обработку других клавиш
            //if (!IsLetterKey(e.KeyCode) && e.KeyCode != Keys.Enter)
            //{
            //    e.Handled = true;
            //}
        }

        private bool IsLetterKey(Keys key)
        {
            // Проверяем латинские буквы
            if (key >= Keys.A && key <= Keys.Z)
                return true;

            // Проверяем русские буквы
            if (key >= Keys.A && key <= Keys.Z && InputLanguage.CurrentInputLanguage.Culture.TwoLetterISOLanguageName == "ru")
                return true;

            return false;
        }

        

        private char GetCharFromKey(Keys key)
        {
            char c = (char)key;

            // Проверяем, если текущий язык - русский
            if (InputLanguage.CurrentInputLanguage.Culture.TwoLetterISOLanguageName == "ru")
            {
                // Сопоставление латинских и русских клавиш
                switch (key)
                {
                    case Keys.A: c = 'ф'; break;
                    case Keys.B: c = 'и'; break;
                    case Keys.C: c = 'с'; break;
                    case Keys.D: c = 'в'; break;
                    case Keys.E: c = 'у'; break;
                    case Keys.F: c = 'а'; break;
                    case Keys.G: c = 'п'; break;
                    case Keys.H: c = 'р'; break;
                    case Keys.I: c = 'ш'; break;
                    case Keys.J: c = 'о'; break;
                    case Keys.K: c = 'л'; break;
                    case Keys.L: c = 'д'; break;
                    case Keys.M: c = 'ь'; break;
                    case Keys.N: c = 'т'; break;
                    case Keys.O: c = 'щ'; break;
                    case Keys.P: c = 'з'; break;
                    case Keys.Q: c = 'й'; break;
                    case Keys.R: c = 'к'; break;
                    case Keys.S: c = 'ы'; break;
                    case Keys.T: c = 'е'; break;
                    case Keys.U: c = 'г'; break;
                    case Keys.V: c = 'м'; break;
                    case Keys.W: c = 'ц'; break;
                    case Keys.X: c = 'ч'; break;
                    case Keys.Y: c = 'н'; break;
                    case Keys.Z: c = 'я'; break;
                }
            }

            return c;
        }

        private void ShowInputDialog(char initialKey)
        {
            using (Form inputForm = new Form())
            {
                TextBox txtInput = new TextBox() { Left = 50, Top = 50, Width = 400, BackColor = SystemColors.Control };
                Button btnOk = new Button() { Text = "OK", Left = 350, Width = 100, Top = 80, DialogResult = DialogResult.OK };
                txtInput.Text = initialKey.ToString(); // Устанавливаем начальное значение TextBox
                txtInput.SelectionStart = txtInput.Text.Length; // Устанавливаем курсор в конец текста

                inputForm.Text = "Поиск";
                inputForm.ClientSize = new Size(500, 150);
                inputForm.AcceptButton = btnOk;
                inputForm.ShowIcon = false;
                inputForm.Controls.AddRange(new Control[] { txtInput, btnOk });
                inputForm.StartPosition = FormStartPosition.CenterScreen;


                if (inputForm.ShowDialog() == DialogResult.OK)
                {
                    string inputText = txtInput.Text;
                    SearchInBaseGrid(inputText);
                }
            }
        }

        private void SearchInBaseGrid(string searchText)
        {
            foreach (DataGridViewRow row in BaseGrid.Rows)
            {
                if (row.Cells["NameBase"].Value != null && row.Cells["NameBase"].Value.ToString().Contains(searchText, StringComparison.OrdinalIgnoreCase))
                {
                    row.Visible = true;
                }
                else
                {
                    row.Visible = false;
                }
            }
        }

        private void label11_Click(object sender, EventArgs e)
        {
            DeletSellGrid();
            UpdateBonusSum();
            UpdateTotalSum();
            CheckSellGridRows();


        }

        private void label10_Click(object sender, EventArgs e)
        {
            ClearSellGrid();
        }
    }
}
