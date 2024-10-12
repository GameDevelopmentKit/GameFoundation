using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using frame8.Logic.Misc.Other.Extensions;
using Com.ForbiddenByte.OSA.Core;

namespace Com.ForbiddenByte.OSA.CustomAdapters.DateTimePicker
{
    /// <summary>
    /// Implementing multiple adapters to get a generic picker which returns a <see cref="DateTime"/> object.
    /// There are 2 ways of using this script: either simply call <see cref="Show(Action{DateTime})"/>
    /// or drag and drop the prefab from /Resources/Com.ForbiddenByte.OSA/DateTimePickerDialog into your scene and subscribe to <see cref="OnDateSelected"/>
    /// </summary>
    public class DateTimePickerDialog : MonoBehaviour
    {
        [SerializeField]                                                                      private bool _AutoInit                   = false;
        [SerializeField]                                                                      private bool _DisplaySelectedDateAsShort = false;
        [SerializeField]                                                                      private bool _DisplaySelectedTimeAsShort = false;
        [Tooltip("If true, animations won't be affected by Time.timeScale")] [SerializeField] private bool _UseUnscaledTime            = true;

        public event Action<DateTime> OnDateSelected;

        public DateTimePickerAdapter DayAdapter   { get; private set; }
        public DateTimePickerAdapter MonthAdapter { get; private set; }
        public DateTimePickerAdapter YearAdapter  { get; private set; }

        public DateTimePickerAdapter HourAdapter   { get; private set; }
        public DateTimePickerAdapter MinuteAdapter { get; private set; }
        public DateTimePickerAdapter SecondAdapter { get; private set; }

        public DateTime SelectedValue =>
            new(this.YearAdapter.SelectedValue,
                this.MonthAdapter.SelectedValue,
                this.DayAdapter.SelectedValue,
                this.HourAdapter.SelectedValue,
                this.MinuteAdapter.SelectedValue,
                this.SecondAdapter.SelectedValue
            );

        private const float SCROLL_DURATION1 = .2f;
        private const float SCROLL_DURATION2 = .35f;
        private const float SCROLL_DURATION3 = .5f;
        private const float ANIM_DURATION    = .25f;
        private const float DEFAULT_WIDTH    = 660f, DEFAULT_HEIGHT = DEFAULT_WIDTH / 2;

        private float AnimElapsedTime01
        {
            get
            {
                var t = Mathf.Clamp01((this.Time - this._AnimStartTime) / ANIM_DURATION);
                return t * t * t * t;
            }
        }

        //Vector3 AnimCurrentScale { get { return Vector3.Lerp(_AnimStart, _AnimEnd, AnimElapsedTime01); } }
        private float AnimCurrentFloat => Mathf.Lerp(this._AnimStart, this._AnimEnd, this.AnimElapsedTime01);

        private float Time => this._UseUnscaledTime ? UnityEngine.Time.unscaledTime : UnityEngine.Time.time;

        private Transform _DatePanel,        _TimePanel;
        private Text      _SelectedDateText, _SelectedTimeText;
        private bool      _Initialized;
        private DateTime? _DateToInitWith;

        private bool _Animating;

        //Vector3 _AnimStart, _AnimEnd;
        private float                   _AnimStart, _AnimEnd;
        private float                   _AnimStartTime;
        private Action                  _ActionOnAnimDone;
        private CanvasGroup             _CanvasGroup;
        private DateTimePickerAdapter[] _AllAdapters = new DateTimePickerAdapter[6];

        public static DateTimePickerDialog Show(Action<DateTime> onSelected)
        {
            return Show(DateTime.Now, onSelected);
        }

        public static DateTimePickerDialog Show(Action<DateTime> onSelected, string prefabPathInResources)
        {
            return Show(DateTime.Now, onSelected, prefabPathInResources);
        }

        public static DateTimePickerDialog Show(DateTime startingDate, Action<DateTime> onSelected)
        {
            return Show(startingDate, onSelected, DEFAULT_WIDTH, DEFAULT_HEIGHT);
        }

        public static DateTimePickerDialog Show(DateTime startingDate, Action<DateTime> onSelected, string prefabPathInResources)
        {
            return Show(startingDate, onSelected, DEFAULT_WIDTH, DEFAULT_HEIGHT, prefabPathInResources);
        }

        public static DateTimePickerDialog Show(DateTime startingDate, Action<DateTime> onSelected, float width, float height)
        {
            var prefabPathInResources = OSAConst.OSA_PATH_IN_RESOURCES + "/" + typeof(DateTimePickerDialog).Name;
            return Show(startingDate, onSelected, width, height, prefabPathInResources);
        }

        public static DateTimePickerDialog Show(DateTime startingDate, Action<DateTime> onSelected, float width, float height, string prefabPathInResources)
        {
            var go     = Resources.Load<GameObject>(prefabPathInResources);
            var picker = (Instantiate(go) as GameObject).GetComponent<DateTimePickerDialog>();
            var c      = FindObjectOfType<Canvas>();
            if (!c) throw new OSAException(typeof(DateTimePickerDialog).Name + ": no Canvas was found in the scene");
            var canvasRT = c.transform as RectTransform;
            var rt       = picker.transform as RectTransform;
            rt.SetParent(canvasRT, false);
            rt.SetAsLastSibling();
            picker._DateToInitWith = startingDate;
            if (onSelected != null) picker.OnDateSelected += onSelected;

            rt.SetInsetAndSizeFromParentEdgeWithCurrentAnchors(RectTransform.Edge.Left, (canvasRT.rect.width - width) / 2, width);
            rt.SetInsetAndSizeFromParentEdgeWithCurrentAnchors(RectTransform.Edge.Top, (canvasRT.rect.height - height) / 2, height);

            return picker;
        }

        private void Awake()
        {
            this._CanvasGroup       = this.GetComponent<CanvasGroup>();
            this._CanvasGroup.alpha = 0f;
            this._AnimEnd           = 1f;
            this._AnimStartTime     = this.Time;
            this._Animating         = true;
        }

        private void Start()
        {
            var adaptersTR = this.transform.Find("Adapters");
            this._DatePanel = adaptersTR.Find("Date");
            this._DatePanel.GetComponentAtPath("SelectedIndicatorText", out this._SelectedDateText);
            var i                                      = 0;
            this._AllAdapters[i++] = this.DayAdapter   = this._DatePanel.GetComponentAtPath<DateTimePickerAdapter>("Panel/Day");
            this._AllAdapters[i++] = this.MonthAdapter = this._DatePanel.GetComponentAtPath<DateTimePickerAdapter>("Panel/Month");
            this._AllAdapters[i++] = this.YearAdapter  = this._DatePanel.GetComponentAtPath<DateTimePickerAdapter>("Panel/Year");
            this._TimePanel        = adaptersTR.Find("Time");
            this._TimePanel.GetComponentAtPath("SelectedIndicatorText", out this._SelectedTimeText);
            this._AllAdapters[i++] = this.HourAdapter   = this._TimePanel.GetComponentAtPath<DateTimePickerAdapter>("Panel/Hour");
            this._AllAdapters[i++] = this.MinuteAdapter = this._TimePanel.GetComponentAtPath<DateTimePickerAdapter>("Panel/Minute");
            this._AllAdapters[i++] = this.SecondAdapter = this._TimePanel.GetComponentAtPath<DateTimePickerAdapter>("Panel/Second");

            for (i = 0; i < this._AllAdapters.Length; ++i) this._AllAdapters[i].Parameters.UseUnscaledTime = this._UseUnscaledTime;

            if (this._AutoInit) this.ExecuteAfter(.2f, this.AutoInit);
        }

        private void Update()
        {
            if (this._Animating)
            {
                //transform.localScale = AnimCurrentFloat;
                this._CanvasGroup.alpha = this.AnimCurrentFloat;

                if (this.AnimElapsedTime01 == 1f)
                {
                    this._Animating = false;
                    if (this._ActionOnAnimDone != null) this._ActionOnAnimDone();
                }

                return;
            }

            if (!this._Initialized) return;

            try
            {
                var current = this.SelectedValue;
                this._SelectedDateText.text = this._DisplaySelectedDateAsShort ? current.ToShortDateString() : current.ToLongDateString();
                this._SelectedTimeText.text = this._DisplaySelectedTimeAsShort ? current.ToShortTimeString() : current.ToLongTimeString();
            }
            catch /*(Exception e)*/
            {
                //Debug.Log(e + "\n"+YearAdapter.SelectedValue + "," + MonthAdapter.SelectedValue + "," + DayAdapter.SelectedValue + "," +
                //HourAdapter.SelectedValue + "," + MinuteAdapter.SelectedValue + "," + SecondAdapter.SelectedValue);
            }
        }

        public void InitWithNow()
        {
            this.InitWithDate(DateTime.Now);
        }

        public void InitWithDate(DateTime dateTime)
        {
            this.StopAnimations();
            this._DateToInitWith = null;
            this.UnregisterAutoCorrection();

            var doneNum    = 0;
            var targetDone = 2;
            Action onDone = () =>
            {
                if (++doneNum == targetDone)
                {
                    this._Initialized = true;
                    this.RegisterAutoCorrection();
                }
            };

            //Func<float, bool> onProgress = p01 =>
            //{
            //	if (p01 == 1f)
            //	{
            //		if (++doneNum == targetDone)
            //		{
            //			_Initialized = true;
            //			RegisterAutoCorrection();
            //		}
            //	}
            //	return true;
            //};

            this.YearAdapter.ResetItems(3000);
            this.YearAdapter.SmoothScrollTo(dateTime.Year - 1, SCROLL_DURATION1, .5f, .5f);
            this.MonthAdapter.ResetItems(12);
            this.MonthAdapter.SmoothScrollTo(dateTime.Month - 1, SCROLL_DURATION2, .5f, .5f);
            this.DayAdapter.ResetItems(DateTime.DaysInMonth(dateTime.Year, dateTime.Month));
            //DayAdapter.SmoothScrollTo(dateTime.Day - 1, SCROLL_DURATION3, .5f, .5f, onProgress, null, true);
            this.DayAdapter.SmoothScrollTo(dateTime.Day - 1, SCROLL_DURATION3, .5f, .5f, null, onDone, true);

            this.SecondAdapter.ResetItems(60);
            this.SecondAdapter.SmoothScrollTo(dateTime.Second, SCROLL_DURATION1, .5f, .5f);
            this.MinuteAdapter.ResetItems(60);
            this.MinuteAdapter.SmoothScrollTo(dateTime.Minute, SCROLL_DURATION2, .5f, .5f);
            this.HourAdapter.ResetItems(24);
            //HourAdapter.SmoothScrollTo(dateTime.Hour, SCROLL_DURATION3, .5f, .5f, onProgress, true);
            this.HourAdapter.SmoothScrollTo(dateTime.Hour, SCROLL_DURATION3, .5f, .5f, null, onDone, true);
        }

        public void ReturnCurrent()
        {
            //_AnimStart = Vector3.one;
            //_AnimEnd = Vector3.zero;
            this._AnimStart     = this._CanvasGroup.alpha;
            this._AnimEnd       = 0f;
            this._AnimStartTime = this.Time;
            this._Animating     = true;

            this._ActionOnAnimDone = () =>
            {
                this._ActionOnAnimDone = null;
                if (this.OnDateSelected != null) this.OnDateSelected(this.SelectedValue);

                Destroy(this.gameObject);
            };
        }

        private void AutoInit()
        {
            this._DateToInitWith = this._DateToInitWith ?? DateTime.Now;
            this.InitWithDate(this._DateToInitWith.Value);
        }

        private void UnregisterAutoCorrection()
        {
            this.YearAdapter.OnSelectedValueChanged  -= this.OnYearChanged;
            this.MonthAdapter.OnSelectedValueChanged -= this.OnMonthChanged;
        }

        private void RegisterAutoCorrection()
        {
            this.YearAdapter.OnSelectedValueChanged  += this.OnYearChanged;
            this.MonthAdapter.OnSelectedValueChanged += this.OnMonthChanged;
        }

        private void OnYearChanged(int year)
        {
            this.OnMonthChanged(this.MonthAdapter.SelectedValue);
        }

        private void OnMonthChanged(int month)
        {
            var selectedDay    = this.DayAdapter.SelectedValue;
            var newDaysInMonth = DateTime.DaysInMonth(this.YearAdapter.SelectedValue, month);
            if (newDaysInMonth == this.DayAdapter.GetItemsCount()) return;
            this.DayAdapter.ResetItems(newDaysInMonth);
            this.DayAdapter.ScrollTo(Math.Min(newDaysInMonth, selectedDay) - 1, .5f, .5f);
        }

        private void StopAnimations()
        {
            foreach (var adapter in this._AllAdapters)
                if (adapter)
                    adapter.CancelAllAnimations();
        }

        private void ExecuteAfter(float seconds, Action action)
        {
            this.StartCoroutine(this.ExecuteAfterCoroutine(seconds, action));
        }

        private IEnumerator ExecuteAfterCoroutine(float seconds, Action action)
        {
            if (seconds > 0f)
            {
                yield return null;
                yield return null;
            }

            if (this._UseUnscaledTime)
                yield return new WaitForSecondsRealtime(seconds);
            else
                yield return new WaitForSeconds(seconds);

            action();
        }
    }
}