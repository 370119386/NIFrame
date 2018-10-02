using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GG
{
    public interface IScene
    {
        bool Create(int iId,ISceneLoader sceneLoader);
        void Enter();
        void Exit();
        int ID();
    }
}