Experimental branch which tests callback-based API.
The new API is implemented in `ElemX.fs`.
When benchmarked it seems 30 percent faster than
the existing API.

But unfortunately the new API extremely hard to use.
One reason why it's hard to use is that F# records are immutable
and so constructing them by modifying fields needs record builder
and writing it manually is hard.

The other reason is that we use the builder to check whether
there are duplicate attributes or whether optional element appears more
than once. Unfortunately this doesn't work for ignored attributes or elements
whose value is not stored into the builder. If the user needs this
functionality then they must implement it manually
for each optional attribute or element.
