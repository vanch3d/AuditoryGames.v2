﻿using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Kindohm.KSynth.Library;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace LSRI.AuditoryGames.AudioFramework
{
    /// <summary>
    /// Extended version of the default sequencer.
    /// 
    /// - Added support for delegates on start, step and end of sequence.
    /// 
    /// @author Nicolas Van Labeke &lt; http://www.lsri.nottingham.ac.uk/nvl/ &gt;
    /// </summary>
    public class SequencerExt : Sequencer
    {
        /// <summary>
        /// 
        /// </summary>
        public delegate void StepStarted();

        /// <summary>
        /// 
        /// </summary>
        public delegate void StepChanged();

        /// <summary>
        /// 
        /// </summary>
        public delegate void StepEnded();

        public event StepStarted _stepStartedHook = null;
        public event StepChanged _stepChangedHook = null;
        public event StepEnded _stepEndedHook = null;

        /// <summary>
        /// 
        /// </summary>
        override protected void ProcessCurrentStep()
        {
            if (this.stepIndex == 0)
            {
                if (_stepStartedHook != null) this._stepStartedHook();
            }
            base.ProcessCurrentStep();
        }

        /// <summary>
        /// 
        /// </summary>
        override protected void ProcessPreSampleTick()
        {
            sampleCounter++;

            if (sampleCounter > samplesPerQuarter)
            {
                sampleCounter = 0;
                this.stepIndex++;
                this.stepChanged = true;
                if (this.stepIndex >= this.StepCount)
                {
                    if (_stepEndedHook != null) this._stepEndedHook();
                    this.stepIndex = 0;

                }
                else
                {
                    if (_stepChangedHook != null) this._stepChangedHook();
                }
            }
        }
    }


    /// <summary>
    /// Abstract wrapper for auditory stimuli generator
    /// 
    /// @author Nicolas Van Labeke &lt; http://www.lsri.nottingham.ac.uk/nvl/ &gt;
    /// </summary>
    public abstract class IFrequencySequencer
    {
        public class Stimulus 
        {
            public Boolean _isSilent = false;
            public int _start = 0;
            public int _duration = 0;
            public double _frequency = 0;

            /// <summary>
            /// 
            /// </summary>
            /// <param name="f"></param>
            /// <param name="s"></param>
            /// <param name="d"></param>
            public Stimulus(double f,int s,int d)
            {
                _isSilent = false;
                _start = s;
                _duration = d;
                _frequency = f;

            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="s"></param>
            /// <param name="d"></param>
            public Stimulus(int s, int d)
            {
                _isSilent = true;
                _start = s;
                _duration = d;
                _frequency = 0;
            }

        }

        /// <summary>
        /// 
        /// </summary>
        protected List<Stimulus> _StimuliStructure = null;

        /// <summary>
        /// Instance of the sequencer to be used for the stimuli generation.
        /// </summary>
        protected SequencerExt _sequencer = null;

        /// <summary>
        /// Reference to the GUI media element.
        /// </summary>
        protected MediaElement _elt = null;

        /// <summary>
        /// Internal storage of voices for the sequencer. 
        /// 
        /// To be used for reset to factory settings.
        /// </summary>
        protected Dictionary<int, ISampleMaker> _intervalVoices = null;

        public abstract void ResetSequencer();


        void _sequencer__stepChangedHook()
        {
            //Debug.WriteLine("SEQUENCER - NEXT STEP : " + this._sequencer.StepIndex);
        }

        void _sequencer__stepEndedHook()
        {
            //Debug.WriteLine("SEQUENCER - LAST STEP ##### : " + this._sequencer.StepIndex);
        }

        void _sequencer__stepStartedHook()
        {
           // Debug.WriteLine("SEQUENCER - STARTED ##### : " + this._sequencer.StepIndex);
        }

        /// <summary>
        /// Default Constructor.
        /// </summary>
        /// <param name="elt">Reference to the media element of the GUI</param>
        protected IFrequencySequencer(MediaElement elt)
        {
            this._elt = elt;
            this._sequencer = new SequencerExt();


            this._sequencer._stepChangedHook += new SequencerExt.StepChanged(_sequencer__stepChangedHook);
            this._sequencer._stepEndedHook += new SequencerExt.StepEnded(_sequencer__stepEndedHook);
            this._sequencer._stepStartedHook += new SequencerExt.StepStarted(_sequencer__stepStartedHook);

            this._intervalVoices = new Dictionary<int, ISampleMaker>();

            SynthMediaStreamSource source = new SynthMediaStreamSource(44100, 2);
            source.SampleMaker = this._sequencer;

            this._sequencer.Tempo = 60;             // 1 beat per second
            this._sequencer.Tempo = 60 * 10;        // 1 beat per 100 ms
            this._sequencer.Tempo = 60 * 10 * 2;    // 1 beat per 50 ms
            this._sequencer.Tempo = 60 * 10 * 4;    // 1 beat per 25 ms
            this._elt.SetSource(source);
        }

        /// <summary>
        /// Initialise the voices of the sequencer.
        /// </summary>
        /// <param name="interIdx"></param>
        protected void setSequencer(int interIdx)
        {

            this._intervalVoices = new Dictionary<int, ISampleMaker>();
            int inc = 0;
            //int intervalNb = 2;// interIdx;

            //double[] inter = { 5000.0, 3000.0, 1000.0, 3000.0 };

            this._sequencer.VoiceCount = 1;
            
            double percent = 10 / 100d;
            uint value = (uint)(15000 * percent);

            //int[] pos = { 0, 2, 7, 2,0,19};
            Stimulus st = null;
            for (int i = 0; i < _StimuliStructure.Count; i++)
            {
                st = _StimuliStructure[i];
                if (st._isSilent) continue;

                IWaveForm wform = new SineWaveForm();
                Oscillator osc1 = new Oscillator();
                osc1.WaveForm = wform;

                Voice voice = new Voice();
                voice.Attenuation = 0;
                voice.Envelope.Attack = value;
                voice.Envelope.Decay = value;
                voice.Oscillators.AddRange(new List<Oscillator>() { osc1 });
                
                voice.Frequency = st._frequency;

                this._intervalVoices.Add(inc++, voice);
                this._sequencer.AddNote(voice, st._start, st._duration);
            }

            this._sequencer.StepCount = st._duration + st._start;
 
/*            for (inc = 0; inc < intervalNb; inc++)
            {
                //Pitch pitch = new Pitch(11, 3);
                IWaveForm form = new SineWaveForm();
                Oscillator osc1 = new Oscillator();
                Oscillator osc2 = new Oscillator();
                osc1.WaveForm = new SineWaveForm();
                osc2.WaveForm = new SineWaveForm();

                Voice voice = new Voice();
                voice.Attenuation = 0;
                voice.Oscillators.AddRange(new List<Oscillator>() { osc1});
                voice.Frequency = inter[inc];//pitch.Frequency;
                voice.Envelope.Attack = value;
                voice.Envelope.Decay = value;
                this._intervalVoices.Add(inc, voice);

                //this.sequencer.AddNote(voice, (3 + 2) * inc + 1, 3);
                this._sequencer.AddNote(voice, pos[2 * inc], pos[2 * inc + 1]);

            }*/

            if (false)
           {
                //Pitch pitch = new Pitch(11, 3);
               IWaveForm form = new SineWaveForm();
               Oscillator osc1 = new Oscillator();
               Oscillator osc2 = new Oscillator();
               osc1.WaveForm = new WhiteNoiseWaveForm();
               osc2.WaveForm = new WhiteNoiseWaveForm();

                Voice voice = new Voice();
                voice.Attenuation = -25;
                voice.Oscillators.AddRange(new List<Oscillator>() {osc1, osc2 });
                voice.Frequency = 3000;
                //voice.Envelope.Attack = value;
                //voice.Envelope.Decay = value;
                this._intervalVoices.Add(inc, voice);

                //this.sequencer.AddNote(voice, (3 + 2) * inc + 1, 3);
               this._sequencer.AddNote(voice, 0, 19);

            }

            
            this._elt.Stop();
            this._sequencer.StepIndex = -1;// this._sequencer.StepCount - 1;
        }

        /// <summary>
        /// 
        /// </summary>
        public void Start()
        {
            if (_elt != null) _elt.Play();
        }

        /// <summary>
        /// 
        /// </summary>
        public void Stop()
        {
            if (_elt != null) _elt.Stop();
            this._sequencer.StepIndex = -1;// this._sequencer.StepCount - 1;
        }


    }

    /// <summary>
    /// Implementation of a 2-Interval stimuli generator
    /// 
    /// @author Nicolas Van Labeke &lt; http://www.lsri.nottingham.ac.uk/nvl/ &gt;
    /// </summary>
    public class Frequency3IGenerator : IFrequencySequencer
    {
        public Frequency3IGenerator(MediaElement elt) : base(elt)
        {
            _StimuliStructure = new List<Stimulus>();
            _StimuliStructure.Add(new Stimulus(5000, 0, 4*2));
            _StimuliStructure.Add(new Stimulus(4 * 2, 10 * 2));
            _StimuliStructure.Add(new Stimulus(3500, 14 * 2, 4 * 2));
            _StimuliStructure.Add(new Stimulus(18 * 2, 10 * 2));
            _StimuliStructure.Add(new Stimulus(3500, 28 * 2, 4 * 2));
            _StimuliStructure.Add(new Stimulus(32 * 2, 40 * 2));
            

            setSequencer(0);
            this._sequencer._stepEndedHook += new SequencerExt.StepEnded(_sequencer__stepEnded3IHook);
        }

        public override void ResetSequencer()
        {
           // setSequencer(3);
        }

        public void ResetSequencer(double a, double b, double c)
        {
            this._sequencer.Reset();
            //myqueue = new Queue<double>();

            _StimuliStructure = new List<Stimulus>();
            _StimuliStructure.Add(new Stimulus(a, 0, 4 * 2));
            _StimuliStructure.Add(new Stimulus(4 * 2, 10 * 2));
            _StimuliStructure.Add(new Stimulus(b, 14 * 2, 4 * 2));
            _StimuliStructure.Add(new Stimulus(18 * 2, 10 * 2));
            _StimuliStructure.Add(new Stimulus(c, 28 * 2, 4 * 2));
            _StimuliStructure.Add(new Stimulus(32 * 2, 40 * 2));

            setSequencer(3);
        }

       /* public delegate void AttachExecuteDelegate();
        public void AttachExecute()
        {
            // here we can modify all the GUI elements; we’re now in the
            // main thread of the application
            if (this._elt != null) this._elt.Stop();
            this._sequencer.StepIndex = 0;// this._sequencer.StepCount - 1;
        }

        public void ThreadProc(object stateInfo)
        {
            if (this._elt != null) this._elt.Stop();
            this._sequencer.StepIndex = 0;// this._sequencer.StepCount - 1;

        }*/

        void _sequencer__stepEnded3IHook()
        {
            //Debug.WriteLine("SEQUENCER - ENDED ## SHUT IT DOWN : " + this._sequencer.StepIndex);
            this._elt.Dispatcher.BeginInvoke(() => this.Stop());
            return;
           /* //ThreadPool.QueueUserWorkItem(new WaitCallback(ThreadProc));
            Voice vc = this._intervalVoices[1] as Voice;
            Stimulus st = this._StimuliStructure[2] as Stimulus;
            if (vc!=null && st!=null)
            {
                //this.//_sequencer.AddNote(voice, st._start, st._duration);#
                this._sequencer.DeleteNote(vc, st._start);
                st._start-=2;
                if (st._start < 5)
                {
                    st._start = 5;
                }
                else
                    this._sequencer.StepCount-=2;

                this._sequencer.AddNote(vc, st._start, st._duration);
                
            }*/
        }
    }

    /// <summary>
    /// Implementation of a 3-Interval stimuli generator
    /// 
    /// @author Nicolas Van Labeke &lt; http://www.lsri.nottingham.ac.uk/nvl/ &gt;
    /// </summary>
    public class Frequency2IGenerator : IFrequencySequencer
    {
        private double _freqBuffer = double.NaN;
        private Queue<double> myqueue = new Queue<double>();

        public Frequency2IGenerator(MediaElement elt) : base(elt)
        {
            //this.sequencer.StepCount = (int)this.stepBox.Value;
            //this._sequencer.Reset();
            ResetSequencer();
           /* _StimuliStructure = new List<Stimulus>();
            _StimuliStructure.Add(new Stimulus(5000, 0, 4 * 2));
            _StimuliStructure.Add(new Stimulus(4 * 2, 10 * 2));
            _StimuliStructure.Add(new Stimulus(3000, 14 * 2, 4 * 2));
            _StimuliStructure.Add(new Stimulus(28 * 2, 20 * 2));

            setSequencer(3);*/
            this._sequencer._stepEndedHook += new SequencerExt.StepEnded(_sequencer__stepEnded2IHook);
            //this._sequencer._stepChangedHook += new SequencerExt.StepChanged(_sequencer__stepEnded2IHook);
        }

        public double GetTrainingFrequency()
        {
            Voice st1 = this._intervalVoices[0] as Voice;
            return st1.Frequency;
        }

        public double GetTargetFrequency()
        {
            Voice st2 = this._intervalVoices[1] as Voice;
            return st2.Frequency;
        }

        public void SetTrainingFrequency(double fq)
        {
            Voice st1 = this._intervalVoices[0] as Voice;
            if (!double.IsNaN(fq) && fq != st1.Frequency)
            {
                st1.Frequency = fq;
            }
        }

        private void SetTargetFrequency(double fq)
        {
           /* if (double.IsNaN(_freqBuffer))
            {
                _freqBuffer = fq;
            }*/
            _freqBuffer = fq;
            myqueue.Enqueue(fq);
            //Debug.WriteLine("FREQUENCY -> queued change to {0}", fq);
        }

        public void ChangeTargetFrequency(double delta)
        {
            double bb = double.NaN;
            if (double.IsNaN(_freqBuffer))
            {
                Voice st1 = this._intervalVoices[1] as Voice;
                bb = st1.Frequency + delta;
            }
            else
                bb = _freqBuffer + delta;

            SetTargetFrequency(bb);
        }

        public void SetTargetFrequency(double fq, bool now)
        {
            SetTargetFrequency(fq);
            if (now)
            {
                //Debug.WriteLine("FREQUENCY -> change now to {0}",fq);
                _sequencer__stepEnded2IHook();
            }
        }

        void _sequencer__stepEnded2IHook()
        {
            //Voice st1 = this._intervalVoices[0] as Voice;
 
           // double last = double.NaN;
           // while (myqueue.Count !=0)
             //  last= myqueue.Dequeue();
            //Debug.WriteLine("Voice in play : {0}", _sequencer.voicesInPlay.Count);
            foreach (VoiceNote item in _sequencer.voicesInPlay)
            {
                Type ff = item.Voice.GetType();

            }

            //_freqBuffer = last;
            Voice st2 = this._intervalVoices[1] as Voice;
            if (!double.IsNaN(_freqBuffer) && _freqBuffer != st2.Frequency)
            {

            /*    if (_sequencer.stepEvents.ContainsKey(_sequencer.StepIndex))
                {
                    StepEvent stepEvent = _sequencer.stepEvents[_sequencer.StepIndex];
                    if (stepEvent != null)
                    {
                        foreach (ISampleMaker key in stepEvent.VoiceNotes.Keys)
                        {
                            VoiceNote nt = stepEvent.VoiceNotes[key];
                            Voice st2s = this._intervalVoices[1] as Voice;
                            if (st2s == nt.Voice)
                            {
                                return;
                            }
                        }
                    }
                }*/
                
                
                st2.Frequency = _freqBuffer;
                _freqBuffer = double.NaN;
                Debug.WriteLine("FREQUENCIES : {0} / {1}", (this._intervalVoices[0] as Voice).Frequency,
                    (this._intervalVoices[1] as Voice).Frequency);
            }
        }

        public override void ResetSequencer()
        {
            //this.sequencer.StepCount = (int)this.stepBox.Value;
            this._sequencer.Reset();
            myqueue = new Queue<double>();
 
            _StimuliStructure = new List<Stimulus>();
            _StimuliStructure.Add(new Stimulus(5000, 0, 4 * 2));
            _StimuliStructure.Add(new Stimulus(4 * 2, 10 * 2));
            _StimuliStructure.Add(new Stimulus(3000, 14 * 2, 4 * 2));
            _StimuliStructure.Add(new Stimulus(28 * 2, 20 * 2));

            setSequencer(3);
        }

   }
}
