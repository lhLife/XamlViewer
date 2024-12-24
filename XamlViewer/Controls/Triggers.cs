
using System.Collections.Specialized;
using System.Globalization;
using Microsoft.UI.Xaml.Markup;

namespace XamlViewer.Controls;



public interface ITriggerValue
{
    bool IsActive { get; }
    event EventHandler? IsActiveChanged;
}




public class EqualsStateTrigger : StateTriggerBase, ITriggerValue
{
    private void UpdateTrigger()
    {
        IsActive = (EqualsStateTrigger.AreValuesEqual(Value, EqualTo, true));
    }

    public object Value
    {
        get { return (object)GetValue(ValueProperty); }
        set { SetValue(ValueProperty, value); }
    }

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register("Value", typeof(object), typeof(EqualsStateTrigger),
        new PropertyMetadata(null, OnValuePropertyChanged));

    private static void OnValuePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var obj = (EqualsStateTrigger)d;
        obj.UpdateTrigger();
    }

    public object EqualTo
    {
        get { return (object)GetValue(EqualToProperty); }
        set { SetValue(EqualToProperty, value); }
    }

    public static readonly DependencyProperty EqualToProperty =
                DependencyProperty.Register("EqualTo", typeof(object), typeof(EqualsStateTrigger), new PropertyMetadata(null, OnValuePropertyChanged));


    internal static bool AreValuesEqual(object value1, object value2, bool convertType)
    {
        if (value1 == value2)
        {
            return true;
        }
        if (value1 != null && value2 != null && convertType)
        {
            // Try the conversion in both ways:
            return ConvertTypeEquals(value1, value2) || ConvertTypeEquals(value2, value1);
        }
        return false;
    }

    private static bool ConvertTypeEquals(object value1, object value2)
    {
        // Let's see if we can convert:
        if (value2 is Enum)
        {
            value1 = ConvertToEnum(value2.GetType(), value1);
        }
        else
        {
            value1 = Convert.ChangeType(value1, value2.GetType(), CultureInfo.InvariantCulture);
        }
        return value2.Equals(value1);
    }

    private static object? ConvertToEnum(Type enumType, object value)
    {
        try
        {
            return Enum.IsDefined(enumType, value) ? Enum.ToObject(enumType, value) : null;
        }
        catch
        {
            return null;
        }
    }

    #region ITriggerValue

    private bool m_IsActive;

    /// <summary>
    /// Gets a value indicating whether this trigger is active.
    /// </summary>
    /// <value><c>true</c> if this trigger is active; otherwise, <c>false</c>.</value>
    public bool IsActive
    {
        get { return m_IsActive; }
        private set
        {
            if (m_IsActive != value)
            {
                m_IsActive = value;
                base.SetActive(value);
                if (IsActiveChanged != null)
                    IsActiveChanged(this, EventArgs.Empty);
            }
        }
    }

    /// <summary>
    /// Occurs when the <see cref="IsActive" /> property has changed.
    /// </summary>
    public event EventHandler? IsActiveChanged;

    #endregion ITriggerValue
}



public class NotEqualStateTrigger : StateTriggerBase, ITriggerValue
{
    private void UpdateTrigger()
    {
        IsActive = !EqualsStateTrigger.AreValuesEqual(Value, NotEqualTo, true);
    }

    public object Value
    {
        get { return (object)GetValue(ValueProperty); }
        set { SetValue(ValueProperty, value); }
    }

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register("Value", typeof(object), typeof(NotEqualStateTrigger),
        new PropertyMetadata(null, OnValuePropertyChanged));

    private static void OnValuePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var obj = (NotEqualStateTrigger)d;
        obj.UpdateTrigger();
    }

    public object NotEqualTo
    {
        get { return (object)GetValue(NotEqualToProperty); }
        set { SetValue(NotEqualToProperty, value); }
    }

    public static readonly DependencyProperty NotEqualToProperty =
                DependencyProperty.Register("NotEqualTo", typeof(object), typeof(NotEqualStateTrigger), new PropertyMetadata(null, OnValuePropertyChanged));

    #region ITriggerValue

    private bool m_IsActive;

    public bool IsActive
    {
        get { return m_IsActive; }
        private set
        {
            if (m_IsActive != value)
            {
                m_IsActive = value;
                base.SetActive(value);
                if (IsActiveChanged != null)
                    IsActiveChanged(this, EventArgs.Empty);
            }
        }
    }

    /// <summary>
    /// Occurs when the <see cref="IsActive" /> property has changed.
    /// </summary>
    public event EventHandler? IsActiveChanged;

    #endregion ITriggerValue
}



[ContentProperty(Name = "StateTriggers")]
public class CompositeStateTrigger : StateTriggerBase, ITriggerValue
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CompositeStateTrigger"/> class.
    /// </summary>
    public CompositeStateTrigger()
    {

        StateTriggers = new StateTriggerCollection();
    }

    private void EvaluateTriggers()
    {
        if (!StateTriggers.Any())
        {
            IsActive = false;
        }
        else if (Operator == LogicalOperator.Or)
        {
            bool result = GetValues().Where(t => t).Any();
            IsActive = (result);
        }
        else if (Operator == LogicalOperator.And)
        {
            bool result = !GetValues().Where(t => !t).Any();
            IsActive = (result);
        }
        else if (Operator == LogicalOperator.OnlyOne)
        {
            bool result = GetValues().Where(t => t).Count() == 1;
            IsActive = (result);
        }
    }

    private IEnumerable<bool> GetValues()
    {
        foreach (var trigger in StateTriggers)
        {
            if (trigger is ITriggerValue)
            {
                yield return ((ITriggerValue)trigger).IsActive;
            }
            else if (trigger is StateTrigger)
            {
                yield return ((StateTrigger)trigger).IsActive;
                //bool? value = null;
                //try
                //{
                //	value = ((StateTrigger)trigger).IsActive;
                //}
                //catch { }
                //if (value.HasValue)
                //	yield return value.Value;
            }
        }
    }

    private void CompositeTrigger_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        OnTriggerCollectionChanged(e.OldItems == null ? null : e.OldItems.OfType<StateTriggerBase>(),
            e.OldItems == null ? null : e.NewItems?.OfType<StateTriggerBase>());
        //TODO: handle reset
    }
    private void CompositeStateTrigger_VectorChanged(Windows.Foundation.Collections.IObservableVector<DependencyObject> sender, Windows.Foundation.Collections.IVectorChangedEventArgs e)
    {
        if (e.CollectionChange == Windows.Foundation.Collections.CollectionChange.ItemInserted)
        {
            var item = sender[(int)e.Index] as StateTriggerBase;
            if (item != null)
            {
                OnTriggerCollectionChanged(null, new StateTriggerBase[] { item });
            }
        }
        //else: Handle remove and reset
    }
    private void OnTriggerCollectionChanged(IEnumerable<StateTriggerBase>? oldItems, IEnumerable<StateTriggerBase>? newItems)
    {
        if (newItems != null)
        {
            foreach (var item in newItems)
            {
                if (item is StateTrigger)
                {
                    long id = item.RegisterPropertyChangedCallback(
                            StateTrigger.IsActiveProperty, TriggerIsActivePropertyChanged);
                    item.SetValue(RegistrationTokenProperty, id);
                }
                else if (item is ITriggerValue)
                {
                    ((ITriggerValue)item).IsActiveChanged += CompositeTrigger_IsActiveChanged;
                }
                else
                {
                    throw new NotSupportedException("Only StateTrigger or triggers implementing ITriggerValue are supported in a Composite trigger");
                }
            }
        }
        if (oldItems != null)
        {
            foreach (var item in oldItems)
            {
                if (item is StateTrigger)
                {
                    var value = item.GetValue(RegistrationTokenProperty);
                    if (value is long)
                    {
                        if (((long)value) > 0)
                        {
                            item.ClearValue(RegistrationTokenProperty);
                            item.UnregisterPropertyChangedCallback(StateTrigger.IsActiveProperty, (long)value);
                        }
                    }
                }
                else if (item is ITriggerValue)
                {
                    ((ITriggerValue)item).IsActiveChanged -= CompositeTrigger_IsActiveChanged;
                }
            }
        }
        EvaluateTriggers();
    }

    private void CompositeTrigger_IsActiveChanged(object? sender, EventArgs e)
    {
        EvaluateTriggers();
    }

    private void TriggerIsActivePropertyChanged(DependencyObject sender, DependencyProperty dp)
    {
        EvaluateTriggers();
    }

    /// <summary>
    /// Gets or sets the state trigger collection.
    /// </summary>
    public StateTriggerCollection StateTriggers
    {
        get { return (StateTriggerCollection)GetValue(StateTriggersProperty); }
        set { SetValue(StateTriggersProperty, value); }
    }

    /// <summary>
    /// Identifies the <see cref="StateTriggers"/> dependency property
    /// </summary>
    public static readonly DependencyProperty StateTriggersProperty =
        DependencyProperty.Register("StateTriggers", typeof(StateTriggerCollection), typeof(CompositeStateTrigger), new PropertyMetadata(null, OnStateTriggersPropertyChanged));

    private static void OnStateTriggersPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        CompositeStateTrigger trigger = (CompositeStateTrigger)d;
        if (e.OldValue is Windows.Foundation.Collections.IObservableVector<DependencyObject> el)
        {
            el.VectorChanged += trigger.CompositeStateTrigger_VectorChanged;
            trigger.OnTriggerCollectionChanged(el.OfType<StateTriggerBase>(), null);
        }

        if (e.NewValue is Windows.Foundation.Collections.IObservableVector<DependencyObject> el2)
        {
            el2.VectorChanged += trigger.CompositeStateTrigger_VectorChanged;
            trigger.OnTriggerCollectionChanged(null, el2.OfType<StateTriggerBase>());
        }
        if (e.NewValue is IEnumerable<StateTriggerBase> el3)
        {
            foreach (var item in el3)
            {
                if (!(item is StateTrigger || !(item is ITriggerValue)))
                {
                    try
                    {
                        throw new NotSupportedException("Only StateTrigger or triggers implementing ITriggerValue are supported in a Composite trigger");
                    }
                    finally
                    {
                        trigger.SetValue(StateTriggersProperty, e.OldValue); //Undo change
                    }
                }
            }
            trigger.CompositeTrigger_CollectionChanged(e.NewValue,
                new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, el3.ToList()));
        }
        trigger.EvaluateTriggers();
    }

    /// <summary>
    /// Used for remembering what token was used for event listening
    /// </summary>
    private static readonly DependencyProperty RegistrationTokenProperty =
        DependencyProperty.RegisterAttached("RegistrationToken", typeof(long), typeof(CompositeStateTrigger), new PropertyMetadata(0));

    /// <summary>
    /// Gets or sets the logical operation to apply to the triggers.
    /// </summary>
    /// <value>The evaluation.</value>
    public LogicalOperator Operator
    {
        get { return (LogicalOperator)GetValue(OperatorProperty); }
        set { SetValue(OperatorProperty, value); }
    }

    /// <summary>
    /// Identifies the <see cref="Operator"/> dependency property
    /// </summary>
    public static readonly DependencyProperty OperatorProperty =
        DependencyProperty.Register("Operator", typeof(LogicalOperator), typeof(CompositeStateTrigger), new PropertyMetadata(LogicalOperator.And, OnEvaluatePropertyChanged));

    private static void OnEvaluatePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        CompositeStateTrigger trigger = (CompositeStateTrigger)d;
        trigger.EvaluateTriggers();
    }

    #region ITriggerValue

    private bool m_IsActive;

    /// <summary>
    /// Gets a value indicating whether this trigger is active.
    /// </summary>
    /// <value><c>true</c> if this trigger is active; otherwise, <c>false</c>.</value>
    public bool IsActive
    {
        get { return m_IsActive; }
        private set
        {
            if (m_IsActive != value)
            {
                m_IsActive = value;
                base.SetActive(value);
                if (IsActiveChanged != null)
                    IsActiveChanged(this, EventArgs.Empty);
            }
        }
    }

    /// <summary>
    /// Occurs when the <see cref="IsActive" /> property has changed.
    /// </summary>
    public event EventHandler? IsActiveChanged;

    #endregion ITriggerValue
}

/// <summary>
/// Logical operations
/// </summary>
public enum LogicalOperator
{
    /// <summary>
    /// And (All must be active)
    /// </summary>
    And,
    /// <summary>
    /// Or (Any can be active)
    /// </summary>
    Or,
    /// <summary>
    /// Only one can be active
    /// </summary>
    OnlyOne
}
/// <summary>
/// Collection for the <see cref="CompositeStateTrigger"/>.
/// </summary>
public sealed class StateTriggerCollection : DependencyObjectCollection { }
