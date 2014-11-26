Strathweb.Roslyn
================

Roslyn refactorings and code actions


### Move Class To File

A refactoring (`Ctrl + .`) which allows you to extract a class into its own file. 

Class is extracted into the same folder that the current file is located in.
The namespace is the same is of the original file; similarly all of the using statements are copied over.
If the class has any comments on it, they are taken as well.

Obviously, it's also removed from the original document.

The refactoring is offered only for classes matching the following requirements:

 - class is not private
 - class name does not match the current filename

![alt tag](http://i.imgur.com/YQnu1rX.png)
