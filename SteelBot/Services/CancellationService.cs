using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SteelBot.Services
{
    public class CancellationService
    {
        private readonly CancellationTokenSource _cts;

        public CancellationService()
        {
            _cts = new CancellationTokenSource();
        }

        public CancellationToken Token => _cts.Token;

        public void Cancel()
        {
            _cts.Cancel();
        }
    }
}
