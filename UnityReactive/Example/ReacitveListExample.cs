using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Jeomseon.Extensions;
using Jeomseon.UnityReactive;
using System.Linq;

public class ReacitveListExample : MonoBehaviour
{
    public ReactiveList<int> Numbers = new();

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

    public void OnChangedElement(ChangedElementMessage<int> message)
    {
        Debug.Log($"Index : {message.Index}");
        Debug.Log($"Previous : {message.PreviousElement}");
        Debug.Log($"New : {message.NewElement}");
    }
}
