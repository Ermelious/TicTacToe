/*
 * Author: Lance
 */
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Occupant : NetworkBehaviour
{
    [SerializeField]
    private GameManager.ShapeType shape;
    public GameManager.ShapeType Shape => shape;
}
