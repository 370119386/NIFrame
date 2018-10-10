using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NI
{
    public interface IScene
    {
        bool Create(int iId);
        void Enter();
        void Exit();
        int ID();
    }
}