using System;

public static class ClipBoard
{
    string Get()
    {
        return $"echo {val} | clip".Bat();
    }
}
