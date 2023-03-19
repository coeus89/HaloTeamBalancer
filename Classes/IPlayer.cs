using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;

namespace Halo_Team_Balancer.Classes
{
    internal interface IPlayer
    {
        Task<string> getRank(Dictionary<string, int> rDict);
    }
}