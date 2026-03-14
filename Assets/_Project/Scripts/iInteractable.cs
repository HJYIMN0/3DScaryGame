using System;

public interface iInteractable
{
    public event Action OnInteractionStart;
    public event Action OnInteractionEnd;
    void InteractWithTask();
}
