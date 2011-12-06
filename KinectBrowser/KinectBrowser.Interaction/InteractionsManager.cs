using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KinectBrowser.Interaction.Gestures;

namespace KinectBrowser.Interaction
{
    public class InteractionsManager
    {
        public IInputClient Client { get; private set; }

        public IInputProvider[] Providers { get; private set; }

        public IInputProvider CurrentProvider { get; private set; }

        public InteractionsManager(IInputClient client)
        {
            if (client == null)
                throw new ArgumentNullException("client");

            Client = client;
        }

        public void Initialize(IInputProvider[] providers)
        {
            if (providers == null || providers.Length < 1)
                throw new ArgumentNullException("providers");

            var validProviders = (from p in providers where p.Available select p).ToArray();

            if (validProviders.Length < 1)
                throw new InvalidOperationException("No providers could be loaded");
            else
                Providers = validProviders;

            CheckCurrentProvider();

            InteractionsCore.Core.Loop += delegate
            {
                Update();
            };
        }

        private bool CheckCurrentProvider()
        {
            if (CurrentProvider == null || !CurrentProvider.Available)
            {
                CurrentProvider = (from p in Providers orderby p.Priority select p).FirstOrDefault(p => p.Available);

                if (CurrentProvider != null)
                {
                    var positionProviders = CurrentProvider.Positions;

                    foreach (var p in Providers)
                    {
                        p.Enabled = p == CurrentProvider;
                    }
                }
                else
                {
                    foreach (var p in Providers)
                        p.Enabled = false;
                }

                return false;
            }
            else
                return true;
        }

        public void Shutdown()
        {
            foreach (var provider in Providers)
                provider.Shutdown();
        }

        public void Update()
        {
            var provider = CurrentProvider;

            if (provider == null || !CheckCurrentProvider())
            {
                /* not yet initialized */
                return;
            }

            provider.Update();

            var positions = provider.Positions;
        }
    }
}
