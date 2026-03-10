using System;

namespace SongDataService;

public class ErrorData
{
    public int error;
    public string message;

    public ErrorData(int error, string message) { this.error = error; this.message = message; }
}
