using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Halo_Team_Balancer.Classes
{
    internal class Player : IPlayer, IComparable<Player>
    {
        public string _name { get; set; }
        public int _csr { get; set; }
        
        public Player() 
        {
            _name = "NewPlayer" + DateTime.Now.Minute.ToString() 
                + DateTime.Now.Second.ToString() 
                + DateTime.Now.Millisecond.ToString();
            _csr = 0;
        }
        public Player(string name, int csr)
        {
            _name += name;
            _csr = csr;
        }

        public int CompareTo(Player? other)
        {
            return _csr.CompareTo(other._csr);
        }

        public override string ToString()
        {
            return _name + ": " + _csr.ToString();
        }

        public async Task<string> getRank(Dictionary<string, int> rDict)
        {
            var keys = rDict.Keys.ToList<string>();
            var values = rDict.Values.ToList<int>();
            string resName = "Gold 1";
            await Task.Factory.StartNew(() => {
                for (int i = 0; i < keys.Count; i++)
                {

                    if (_csr < values[i])
                    {
                        resName = keys[i - 1];
                    }
                    else
                    {
                        resName = keys[i];
                    }
                }
            });
            
            return resName;
        }
    }
}
