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

        public GestureSequenceManager GestureSequenceManager { get; private set; }

        public IInputProvider[] Providers { get; private set; }

        public IInputProvider CurrentProvider { get; private set; }

        public TimeSpan GestureRecognitionLatency { get; set; }

        private List<RecognizedGesture> RecognizedGestures = new List<RecognizedGesture>();

        public InteractionsManager(IInputClient client)
        {
            if (client == null)
                throw new ArgumentNullException("client");

            Client = client;
            GestureRecognitionLatency = TimeSpan.FromMilliseconds(500);
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

                    GestureSequenceManager = new GestureSequenceManager(
                        (from position in positionProviders select new GestureManager(position.Id)).ToArray(), TimeSpan.FromSeconds(5));

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

        public void RecordSingleRecognizedGesture(string key, EventHandler actionDelegate, params SimpleGesture[] gestures)
        {
            if (actionDelegate != null)
            {
                var gestureElements = new Dictionary<string, SimpleGesture[]>();
                gestureElements[key] = gestures;
                var gestureContent = new RecognizedGesture(gestureElements);
                gestureContent.Activated += delegate(object sender, EventArgs e)
                {
                    actionDelegate(sender, e);
                };
                RecordRecognizedGesture(gestureContent);
            }
        }

        public void RecordRecognizedGesture(RecognizedGesture gesture)
        {
            if (gesture == null)
                throw new ArgumentNullException("gesture");

            if (!RecognizedGestures.Contains(gesture))
                RecognizedGestures.Add(gesture);
        }

        public void Update()
        {
            var provider = CurrentProvider;

            if (provider == null || GestureSequenceManager == null || !CheckCurrentProvider())
            {
                /* not yet initialized */
                return;
            }

            var positions = provider.Positions;

            GestureSequenceManager.Update(positions, Client.ClientArea);

            var now = DateTime.Now;
            
            /* Maximize Test Sequence */
            var currentSequence = GestureSequenceManager.CurrentSequence;

            if ((now - currentSequence.LastModificationTime) >= GestureRecognitionLatency)
            {
                bool hasAValidGesture = false;

                foreach (var item in RecognizedGestures)
                {
                    var allValid = true;
                    var score = 0.0;

                    foreach (var machine in item.Machines)
                    {
                        var machineInst = machine.Value;
                        machineInst.Reset();

                        for (int i = 0; i < GestureSequenceManager.CurrentSequence.Count && !machineInst.Valid; i++)
                        {
                            var gesture = GestureSequenceManager.CurrentSequence[GestureSequenceManager.CurrentSequence.Count - i - 1].GetById(machine.Key);
                            if (gesture != null)
                            {
                                machineInst.Update(gesture);
                            }
                        }

                        allValid = allValid && machineInst.Valid;

                        if (!allValid)
                            break;
                        else
                            score += machineInst.Score;
                    }

                    item.LastScore = allValid ? score : 0;
                    hasAValidGesture = hasAValidGesture || allValid;
                }

                if (hasAValidGesture)
                {
                    var recognizedGesture = (from g in RecognizedGestures where g.LastScore > 0 select g).OrderByDescending(g => g.LastScore).FirstOrDefault();

                    if (recognizedGesture != null)
                    {
                        recognizedGesture.RaiseActivated();

                        GestureSequenceManager.CurrentSequence.Reset();
                    }
                }
            }
        }
    }
}
