using Rainbow.Model;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IParticipantsList
{
    public void AddParticipant(Contact c, bool isMe);
    public void ClearAll();
    public void RemoveParticipant(Contact c);
}

public class AbstractParticipantsList : MonoBehaviour, IParticipantsList
{
    public virtual void AddParticipant(Contact c, bool isMe)
    {
    }

    public virtual void ClearAll()
    {
    }

    public virtual void RemoveParticipant(Contact c)
    {        
    }
     
}
