## Introduction

Ping.Pong is the result of a series of blog posts centered around building a push app written in Silverlight.  It focuses predominantly on the streaming API to demonstrate out to use Rx to process a large number of messages.

## Features

A very minimal set of the standard set of Twitter options have been implemented.  By default it will pull the home and mention timelines.  You can also update your status.  That's it for now...no DM, no RT.  It's still very early.

However...

## Streaming

This is an awesome feature that removes all rate limits.  Tweets are streamed to the client as they happen in real-time.

## Search

The top right of the application contains a text box for searching.  The syntax is this:

	buildwin|bldwin silverlight

That will create two additional columns, one which will contain any tweet container buildwin or bldwin, and the other which filters for silverlight.

## Blog Series

The full set of blog post series about Ping.Pong starts [here](http://blingcode.blogspot.com/2011/08/building-real-time-push-app-with.html)

## License

All source code is released under the [Ms-PL](http://www.opensource.org/licenses/ms-pl) license.
