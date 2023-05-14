if(typeof console === "undefined" || console === null)
{
    console =
    {
        log: function(msg) { alert(msg); },
        info: function(msg) { alert(msg); },
        warn: function(msg) { alert(msg); },
        error: function(msg) { alert(msg); }
    };
}