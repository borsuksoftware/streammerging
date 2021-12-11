## Stream Merging
The library (available as a nuget package - BorsukSoftware.Utils.StreamMerging) is designed to make it easier to process (read only) multiple streams as a single chunk.

#### Suggested use cases
We had a use-case where we had large files which were broken up into smaller chunks and which were subsequently compressed and stored individually on disc. Usually, consumers wanted to consume a single chunk but there was also the requirement to be able to operate over the original file. This would have either required us to load in the large file before processing or to create the library. 

Note that it only supports reading and not writing and seek is not supported.
#### FAQs

###### Why doesn't the package currently support .net standard2.0?
We wanted to and we probably will in the future. However, .netstandard 2.0 doesn't support the necessary Span<..> / Memory<..> data types (unlike 2.1) so we had the choice of either conditional compilation with 2 different builds, one for .net standard 2.0 and one for 2.1, or go with 2.1 only. If 2.0 support is needed, feel free to either copy the source code into your own project or raise an issue (do feel free to create the PR as well :-) ) and we'll get it updated

###### Why doesn't the code support being able to seek to a given location?
Our use-case was that we are interested in a single pass through a subset of the inputs whilst minimising resource consumption on our processes. As part of that, we want to avoid holding onto the streams once they've been processed. There's no reason why an additional class couldn't be created which retained a reference to previously used streams and thus allow people to go back, but as yet, we don't have that use-case. If you do, then raise an issue (and PR ideally :-) ) and we'll see what we can do.

###### I have a bug, what do I do?
Talk to us / raise an issue in the project and we'll see what we can do.

