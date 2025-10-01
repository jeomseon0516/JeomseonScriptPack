using Jeomseon.Extensions;
using Jeomseon.UnityReactive;
using System;
using System.Linq;
using UnityEditor.Presets;
using UnityEngine;
using UnityEngine.Events;

public interface IExampleInterface
{ }

[Serializable]
public class ExampleInterface : IExampleInterface 
{
    public int count;
}

public class ReacitveListExample : MonoBehaviour
{
    public ReactiveList<int> Numbers = new();
    public ReactiveList<ExampleInterface> Example = new();

    public UnityEvent<int> NumEvent = new();

    public IReadOnlyReactiveList<IExampleInterface> PExample => Example;

    private void Start()
    {
        Numbers.Add(0);
        Numbers.ForEach(num => Debug.Log(num));
        Numbers.Add(1);
        Numbers.ForEach(num => Debug.Log(num));
        Numbers.Add(2);
        Numbers.ForEach(num => Debug.Log(num));
        Numbers.Add(2);
        Numbers.ForEach(num => Debug.Log(num));
        Numbers.Add(3);
        Numbers.ForEach(num => Debug.Log(num));

        Numbers.Remove(0);
        Numbers.ForEach(num => Debug.Log(num));
        Numbers.RemoveAll(num => num == 2);
        Numbers.ForEach(num => Debug.Log(num));
        Numbers.InsertRange(1, new int[] { 10, 11, 12, 13, 14, 15 });
        Numbers.ForEach(num => Debug.Log(num));

        Numbers.RemoveRange(2, Numbers.Count - 2);
        Numbers.ForEach(num => Debug.Log(num));

        Numbers.RangeMode = RangeEventMode.BATCHED;

        Numbers.AddRange(new int[] { 100, 101, 102, 103, 104 });
        Numbers.ForEach(num => Debug.Log(num));

        Numbers.InsertRange(1, new int[] { 10, 11, 12, 13, 14, 15 });
        Numbers.ForEach(num => Debug.Log(num));

        Numbers.RemoveRange(2, Numbers.Count - 2);
        Numbers.ForEach(num => Debug.Log(num));

        foreach (var num in Numbers)
        {
            Debug.Log(num);
        }

        int[] numArray = Numbers
            .Where(n => n == 0)
            .ToArray();

        numArray.ForEach(n => Debug.Log(n));

        Numbers[0] = 15;
        Numbers.ChangedEvent -= OnChangedElement;
        Numbers[0] = 10;

        NumEvent.RemoveListener(OnAddedElement);
    }

    public void OnAddedElement(int element)
    {
        Debug.Log($"Add: {element}");
    }

    public void OnRemovedElement(int element)
    {
        Debug.Log($"Remove: {element}");
    }

    public void OnAddedRange(int[] nums)
    {
        nums.ForEach(OnAddedElement);
    }

    public void OnRemovedRange(int[] nums)
    {
        nums.ForEach(OnRemovedElement);
    }

    public void OnChangedElement(int index, int prev, int now)
    {
        Debug.Log($"Index : {index}");
        Debug.Log($"Previous : {prev}");
        Debug.Log($"New : {now}");
    }
}
