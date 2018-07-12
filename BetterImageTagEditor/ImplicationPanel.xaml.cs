using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BetterImageTagEditor
{
    public partial class ImplicationPanel : DockPanel
    {
        private Database DB;
        private TextBox NewBefore;
        private TextBox NewAfter;

        public ImplicationPanel(Database database)
        {
            InitializeComponent();
            DB = database;
            RedrawImplicationList();
        }

        private void RedrawImplicationList()
        {
            ImplicationList.Items.Clear();

            // Draw list of existing implications
            foreach (KeyValuePair<string, string> impl in DB.ImplicationTable)
            {
                DisplayImplication(impl.Key, impl.Value);
            }

            // Draw empty textboxes for new sub input
            DisplayImplication("", "");
        }

        private void DisplayImplication(string before, string after)
        {
            // Add new row
            DockPanel dockPanel = new DockPanel();
            dockPanel.Tag = before;

            // Add textbox for first tag
            TextBox first = new TextBox();
            first.Text = before;
            first.MinWidth = 200;
            dockPanel.Children.Add(first);

            // Add arrow
            TextBlock arrow = new TextBlock();
            arrow.Text = "->";
            arrow.Margin = new Thickness(4, 0, 4, 0);
            dockPanel.Children.Add(arrow);

            // Add textbox for second tag
            TextBox second = new TextBox();
            second.Text = after;
            second.MinWidth = 200;
            dockPanel.Children.Add(second);

            // Add delete/confirm button
            Button button = new Button();
            button.Width = 20;
            button.Margin = new Thickness(4, 0, 0, 0);
            if (before != "")
            {
                button.Content = "x";
                button.Background = new SolidColorBrush(Color.FromArgb(20, 200, 0, 0));
                button.Click += Delete_Click;

                // Also handle user modifying text
                first.LostFocus += TextBox_LostFocus;
                second.LostFocus += TextBox_LostFocus;
            }
            else
            {
                button.Content = "+";
                button.Background = new SolidColorBrush(Color.FromArgb(20, 0, 200, 0));
                button.Click += Confirm_Click;
            }
            dockPanel.Children.Add(button);

            // Add row to panel
            ImplicationList.Items.Add(dockPanel);

            // Keep reference to last (empty) pair of textboxes
            NewBefore = first;
            NewAfter = second;
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            // Get sub to delete
            Button delete = (Button)sender;
            DockPanel dp = (DockPanel)delete.Parent;
            string tag = (string)dp.Tag;

            // Remove sub from database
            DB.RemoveImplication(tag);
            RedrawImplicationList();
        }

        // Update substitution in database
        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            // Get new values
            TextBox tb = (TextBox)sender;
            DockPanel dp = (DockPanel)tb.Parent;
            string before = ((TextBox)dp.Children[0]).Text;
            string after = ((TextBox)dp.Children[2]).Text;

            // Make sure new values are valid
            if (!DB.IsValidTag(before) || !DB.IsValidTag(after))
            {
                return;
            }

            // Check if values have changed
            string tag = (string)dp.Tag;
            if (tag == before && DB.ImplicationTable[tag] == after)
            {
                return;
            }

            // Remove original implication from database
            DB.RemoveImplication(tag);

            // Add updated implication to database
            DB.AddImplication(before, after);
            dp.Tag = before;
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            // Only accept valid tags
            if (!DB.IsValidTag(NewBefore.Text) || !DB.IsValidTag(NewAfter.Text))
            {
                return;
            }

            // Add new sub to database
            DB.AddImplication(NewBefore.Text, NewAfter.Text);
            RedrawImplicationList();
        }
    }
}
