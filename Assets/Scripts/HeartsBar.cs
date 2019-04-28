using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HeartsBar : MonoBehaviour
{
    public Sprite fullheart;

    public Sprite emptyheart;

    public Image heartPrefab;

    [SerializeField]
    private int _initialHeartTotal;

    public int total
    {
        get
        {
            return _hearts.Count;
        }
        set
        {
            RefreshTotalIfNeeded(value);
        }
    }

    private List<Image> _hearts = new List<Image>();

    [SerializeField]
    private int _initialHeartCurrent;

    private int _current;
    public int current
    {
        get
        {
            return _current;
        }
        set
        {
            RefreshCurrentIfNeeded(value);
        }
    }

    private void Awake()
    {
        total = _initialHeartTotal;
        current = _initialHeartCurrent;
    }

    private void RefreshTotalIfNeeded(int newTotal)
    {
        if (newTotal == _hearts.Count)
            return;

        for (int i = 0; i < _hearts.Count; i++)
        {
            Destroy(_hearts[i].gameObject);
        }

        _hearts.Clear();
        for (int i = 0; i < newTotal; i++)
        {
            _hearts.Add(Instantiate<Image>(heartPrefab, this.transform));
        }

        RefreshCurrentIfNeeded(_current, true);
    }

    private void RefreshCurrentIfNeeded(int newCurrent, bool force = false)
    {
        if (!force && newCurrent == _current)
            return;

        for (int i = 0; i < _hearts.Count; i++)
        {
            _hearts[i].sprite = i < newCurrent ? fullheart : emptyheart;
        }

        _current = newCurrent;
    }
}
