using System.Collections.Generic;

public class ParticipantCardComparer : IComparer<ParticipantCard>
{
    public int Compare(ParticipantCard x, ParticipantCard y)
    {
        if( x.Issharing) return -1;
        if( y.Issharing ) return 1;
        return x.Contact.DisplayName.CompareTo(y.Contact.DisplayName);
    }
}
