using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NI
{
    public interface IScene
    {
        bool Create(object argv);
        void Enter();
        void Exit();
        int ID();
    }
}