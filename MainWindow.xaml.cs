using Halo_Team_Balancer.Classes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using Excel = Microsoft.Office.Interop.Excel;


namespace Halo_Team_Balancer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Dictionary<string, Player> playerDict;
        Dictionary<string, int> rankDict;
        Dictionary<string, Player> selectedPlayerDict;
        Task loadPlayers;
        Task loadRanks;
        delegate void DelMessageBox(string message);
        DelMessageBox delMsg = null;
        delegate void DelPlayers(List<string> players);
        DelPlayers delShowPlayers = null;
        DelPlayers delShowSelectedPlayers = null;
        delegate void DelUpdateResults(string txt);
        DelUpdateResults delUpdateResultsTextBox = null;
        delegate void DelUpdateRB(List<Player> Blue, List<Player> Red);
        DelUpdateRB delUpdateRBTextBox = null;
        Action delClrTxt;

        public MainWindow()
        {
            InitializeComponent();
            playerDict = new Dictionary<string, Player>();
            rankDict = new Dictionary<string, int>();
            loadPlayers = Task.Run(() => LoadPlayersAsync(playerDict));
            loadRanks = Task.Run(() => LoadRanksAsync(rankDict));
            delShowPlayers = new DelPlayers(UpdatePlayersListBox);
            delUpdateResultsTextBox = new DelUpdateResults(UpdateResultTextBox);
            selectedPlayerDict = new Dictionary<string, Player>();
            delShowSelectedPlayers = new DelPlayers(UpdateSortListBox);
            delMsg = new DelMessageBox(ShowMessageBox);
            delUpdateRBTextBox = new DelUpdateRB(UpdateRBListBoxes);
            delClrTxt = new Action(clear_Teams);
        }



        private async void LoadPlayersAsync(Dictionary<string, Player> pDict)
        {
            pDict.Clear();
            string folder = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
            //string folder = "D:\\Projects\\Halo_Team_Balancer";
            folder += "\\settings\\Players.csv";
            using (var reader = new StreamReader(folder))
            {
                // Skip the header row
                await reader.ReadLineAsync();

                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync();
                    var values = line.Split(',');

                    Player player = new Player(values[0], Int32.Parse(values[1]));
                    pDict.Add(player._name, player);
                }
                reader.Close();
            }
            this.Dispatcher.Invoke(delShowPlayers, pDict.Keys.ToList<string>());
        }

        private async Task LoadRanksAsync(Dictionary<string, int> rDict)
        {
            string folder = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
            folder += "\\settings\\Ranks.csv";
            using (var reader = new StreamReader(folder))
            {
                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync();
                    var values = line.Split();

                    rDict.Add(values[0], Int32.Parse(values[1]));

                }
            }
        }

        private async void SeparateTeamsButton_ClickAsync(object sender, RoutedEventArgs e)
        {
            if (!loadPlayers.IsCompletedSuccessfully)
            {
                throw new TaskSchedulerException("Not Finished Loading Players");
            }

            // Get input list of numbers from text box
            List<Player> players = selectedPlayerDict.Values.ToList();
            

            // Disable the button and show a "working" message
            SeparateTeamsButton.IsEnabled = false;
            this.Dispatcher.Invoke(delUpdateResultsTextBox, "Working...");
            if (players.Count < 2)
            {
                throw new ApplicationException("You need to have at lease two players " +
                    "listed in order to sort them into teams.");
            }

            try
            {
                // Call the separate lists method on a separate thread
                var (list1, list2) = await Task.Run(() => SeparateLists(players));

                // Calculate the averages of the two lists
                double average1 = (from p in list1 select p._csr).ToList<int>().Average();
                double average2 = (from p in list2 select p._csr).ToList<int>().Average();
                double totalAvg = (from p in players select p._csr).ToList<int>().Average();

                // Display the two lists and their averages in the result text box
                string teamtext = $"Blue Team ({list1.Count}): (average csr: {average1:F2})\n" +
                                      $"Red Team ({list2.Count}): (average csr: {average2:F2})";
                await this.Dispatcher.BeginInvoke(delUpdateResultsTextBox, teamtext);
                await this.Dispatcher.BeginInvoke(delUpdateRBTextBox, list1, list2);
                
            
            }
            catch (TaskSchedulerException tex)
            {
                this.Dispatcher.Invoke(delUpdateResultsTextBox, tex.Message);
                //ResultTextBox.Text = tex.Message;
            }
            catch (ApplicationException aex)
            {
                this.Dispatcher.Invoke(delUpdateResultsTextBox, aex.Message);
                //ResultTextBox.Text = aex.Message;
            }
            catch (Exception ex)
            {
                // Display any errors in the result text box
                //ResultTextBox.Text = $"Error: {ex.Message}";
                this.Dispatcher.Invoke(delUpdateResultsTextBox, ex.Message);
            }

            // Re-enable the button
            SeparateTeamsButton.IsEnabled = true;
        }

        private static (List<Player> list1, List<Player> list2) SeparateLists(List<Player> players)
        {
            // Calculate the total sum and average of the input list
            double average = (from p in players select p._csr).ToList<int>().Average();

            // Sort the numbers in descending order
            players.Sort((a, b) => b.CompareTo(a));

            // Create two new lists and initialize them with the first element
            var list1 = new List<Player> { players[0] };
            var list2 = new List<Player> { players[1] };

            // Distribute the remaining numbers to the two lists
            for (int i = 2; i < players.Count; i++)
            {
                if ((from p in list1 select p._csr).ToList<int>().Sum() < 
                    (from p in list2 select p._csr).ToList<int>().Sum())
                {
                    list1.Add(players[i]);
                }
                else
                {
                    list2.Add(players[i]);
                }
            }

            // Return the two lists
            return (list1, list2);
        }

        private void UpdateRBListBoxes(List<Player> BlueTeam, List<Player> RedTeam)
        {
            BlueTeamListBox.ItemsSource = BlueTeam;
            RedTeamListBox.ItemsSource = RedTeam;
        }

        private void UpdatePlayersListBox(List<string> players)
        {
            PlayersListBox.ItemsSource = players;

        }

        private void UpdateResultTextBox(string txt)
        {
            ResultTextBox.Text = txt;

        }

        private void UpdateSortListBox(List<string> players)
        {
            SelectedPlayerListBox.ItemsSource = players;

        }

        private void ShowMessageBox(string msg)
        {
            MessageBox.Show(msg);
        }

        private async Task<bool> AddToSelectedPlayerListAsync(ListBox listBox)
        {
            try
            {
                if (listBox != null)
                {
                    int index = listBox.SelectedIndex;
                    //MessageBox.Show("You clicked on item " + index);
                    string pname = (playerDict.Keys.ToList<string>())[index];
                    selectedPlayerDict.Add(pname, playerDict[pname]);
                    await this.Dispatcher.BeginInvoke(delShowSelectedPlayers, selectedPlayerDict.Keys.ToList<string>());
                }
            }
            catch (ArgumentException aex)
            {
                this.Dispatcher.Invoke(delMsg, "you've already added that player to the teams list.");
                //MessageBox.Show(@"you've already added that player to the teams list.");
            }
            
            return true;
        }

        private async void PlayersListBox_MouseDoubleClick_Async(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ListBox listBox = sender as ListBox;
            await AddToSelectedPlayerListAsync(listBox);
        }

        private async void PlayersListBox_KeyDown_Async(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter) 
            {
                ListBox listBox = sender as ListBox;
                await AddToSelectedPlayerListAsync(listBox);
            }
        }

        private void clear_Teams()
        {
            selectedPlayerDict = new Dictionary<string, Player>();
            List<Player> emptyTeam = new List<Player>();
            RedTeamListBox.ItemsSource = emptyTeam;
            BlueTeamListBox.ItemsSource = emptyTeam;
            ResultTextBox.Text = string.Empty;
            SelectedPlayerListBox.ItemsSource = emptyTeam;
        }

        private async void ClearTeamsButton_Click_Async(object sender, RoutedEventArgs e)
        {
            await this.Dispatcher.BeginInvoke(delClrTxt);
        }

        private async void OpenCSVButton_Click_Async(object sender, RoutedEventArgs e)
        {
            string file_path = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
            //string folder = "D:\\Projects\\Halo_Team_Balancer";
            file_path += "\\settings\\Players.csv";

            await Task.Run(() => 
            {
                try
                {
                    Excel.Application objExcel = new Excel.Application();
                    objExcel.Workbooks.OpenText(file_path, Comma: true);
                    objExcel.Visible = true;
                }
                catch (Exception)
                {
                    //this.Dispatcher.Invoke(delMsg, "Something happened and " +
                    //    "you were unable to open the csv file with excel. " +
                    //    "Ensure you have microsoft excel installed and registered." +
                    //    ex.Message);
                    Process.Start("notepad.exe", file_path);
                }
                finally
                {
                    
                }
            });
            
        }

        private async void RefreshButton_Click_Async(object sender, RoutedEventArgs e)
        {
            loadPlayers = Task.Run(() => LoadPlayersAsync(playerDict));
            await loadPlayers;
        }
    }

}
