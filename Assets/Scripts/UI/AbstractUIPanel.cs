/*
 * Author: Lance
 */
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public abstract class AbstractUIPanel : MonoBehaviour
{
    public static List<AbstractUIPanel> panels = new List<AbstractUIPanel>();

    public virtual void Awake()
    {
        panels.Add(this);
    }

    private void OnDestroy()
    {
        panels.Remove(this); 
    }

    /// <summary>
    /// Enables / Disables UI panels based of the specified params.
    /// </summary>
    /// <param name="status"></param>
    /// <param name="disableAllOtherPanels"></param>
    public void SetStatus(bool status, bool disableAllOtherPanels = false)
    {
        if (disableAllOtherPanels)
            foreach (var panel in panels)
                panel.gameObject.SetActive(false);

        gameObject.SetActive(status);
    }
}
