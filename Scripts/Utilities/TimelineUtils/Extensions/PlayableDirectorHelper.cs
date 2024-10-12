// ReSharper disable DelegateSubtraction

namespace GameFoundation.Scripts.Utilities.TimelineUtils.Extensions
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Playables;

    public class PlayableDirectorHelper : MonoBehaviour
    {
        [SerializeField] private PlayableDirector playableDirector;
        private                  Action<double>   onUpdate;

        public event Action<double> OnUpdate
        {
            add
            {
                this.onUpdate += value;
                this.registeredActions.Add(value);
            }
            remove
            {
                this.onUpdate -= value;
                this.registeredActions.Remove(value);
            }
        }

        private List<Action<double>> registeredActions;
        public  List<Action<double>> RegisteredActions => this.registeredActions ?? (this.registeredActions = new());

        [SerializeField] private float timeScale;

        /// <summary>
        /// Custom timescale to iterate the timeline.
        /// </summary>
        public float TimeScale
        {
            get => this.timeScale;
            set
            {
                this.timeScale = value;
                if (Math.Abs(this.timeScale - 1f) > 0.001f) this.playableDirector.timeUpdateMode = DirectorUpdateMode.Manual;
            }
        }

        [SerializeField] private double startTime;

        public double StartTime { get => this.startTime; set => this.startTime = value; }

        [SerializeField] private double endTime;

        public double EndTime { get => this.endTime; set => this.endTime = value; }

        public PlayableDirector PlayableDirector => this.playableDirector;

        private void Awake()
        {
            this.playableDirector  = this.GetComponent<PlayableDirector>();
            this.registeredActions = new();
            this.timeScale         = 1f;
        }

        // Update is called once per frame
        private void Update()
        {
            if (this.playableDirector == null) return;

            if (this.playableDirector.timeUpdateMode == DirectorUpdateMode.Manual)
            {
                this.playableDirector.time += Time.deltaTime * this.TimeScale;
                this.playableDirector.Evaluate();
            }

            this.onUpdate?.Invoke(this.playableDirector.time);
        }
    }
}