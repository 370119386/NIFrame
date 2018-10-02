using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NI
{
    public interface IFrame
    {
        void Create(object argv);
        int GetLayer();
        void Open(int typeId,int frameId,int moduleId,int layer,GameObject root);
        void Close();
    }   
}