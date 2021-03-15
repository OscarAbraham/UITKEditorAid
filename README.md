# UITK Editor Aid
NOTE: If you are using Unity 2020.2 or newer, it's recommended to use [version 2 of this package](https://github.com/OscarAbraham/UITKEditorAid).

Elements and scripts that help in making Unity editors with UIToolkit.<br/>

[UIToolkit](https://docs.unity3d.com/Manual/UIElements.html) (UITK) allows for interfaces that are more dynamic and 
performant than [IMGUI](https://docs.unity3d.com/Manual/GUIScriptingGuide.html). Its web-like API makes creating 
complex Editor UI (i.e. node graphs) a lot easier.

There's a problem, though: the editor part of UITK currently lacks some of the IMGUI features required for easily
creating anything more than basic stuff. This issue can be avoided a little by using IMGUI containers inside UITK,
but projects can easily get too limited and unwieldy, and performance drops quickly when there are multiple instances
of them.

This package contains some of the stuff I use to solve the problem with a pure UIToolkit approach.
<br/><br/>

## How to install
You can either download this package directly to your project's Assets folder, or you can use the Package Manager.
Installing with the __Package Manager__ is very easy:
1. In Unity, go to __Window > Package Manager__.
2. Click the __➕▾__ in the top left and choose __Add Package from git URL__
3. Enter `https://github.com/OscarAbraham/UITKEditorAid.git#v1` and press __Add__.


## Some of the stuff included
Click a name to go to the relevant documentation page for usage info and some code examples:

### [EditableLabel](https://artehacker.com/UITKEditorAid/v1/api/ArteHacker.UITKEditorAid.EditableLabel.html)
A label that tranforms into a text field to be edited. It's edited with a double-click by default.<br/>
![EditableLabel preview](doc_images/EditableLabel.png)

### [ArrayPropertyField](https://artehacker.com/UITKEditorAid/v1/api/ArteHacker.UITKEditorAid.ArrayPropertyField.html)
A UITK version of the good old reorderable list, very customizable. Here's what it looks like by default:

![ArrayPropertyField preview](doc_images/DefaultReorderableList.png)

There's also an abstract [ListControl](https://artehacker.com/UITKEditorAid/v1/api/ArteHacker.UITKEditorAid.ListControl.html)
class that you can use to create your own lists that don't depend on a SerializedProperty
 
### [ManagedReferenceField](https://artehacker.com/UITKEditorAid/v1/api/ArteHacker.UITKEditorAid.ManagedReferenceField.html)
Currently, PropertyFields from members with the [SerializeReference](https://docs.unity3d.com/ScriptReference/SerializeReference.html)
attribute break when they change type. This element is like a PropertyField that updates when the type changes.<br/>
The next gif shows a customized ArrayPropertyField that uses ManagedReferenceFields for its items. Notice that the
interface updates itself when the elements change type due to being reordered:

![A customized list of Managed References](doc_images/ManagedRefsList.png)

There's also a [ManagedReferenceTypeTracker](https://artehacker.com/UITKEditorAid/v1/api/ArteHacker.UITKEditorAid.ManagedReferenceTypeTracker.html)
that can be used for more low level stuff.

### [Rebinder](https://artehacker.com/UITKEditorAid/v1/api/ArteHacker.UITKEditorAid.Rebinder.html)
There are elements that need to be bound again (`VisualElement.Bind(serializedObject)`) to be updated in UIToolkit. For example,
items that are added to a list, or fields with the `[SerializeReference]` attribute that change type. The problem is
that each element that is bound separately has an important performance cost. The Rebinder element solves that by
binding the whole hierarchy every time an update is needed. It also throttles rebinding requests, putting together 
multiple update calls for better performance. 

Relevant elements in this library already use it: ArrayPropertyFields have better performance when they are inside a
Rebinder, and elements related to ManagedReferences need to be inside a Rebinder to work.

You can use the Rebinder yourself by calling the
[RequestRebind](https://artehacker.com/UITKEditorAid/v1/api/ArteHacker.UITKEditorAid.Rebinder.html#ArteHacker_UITKEditorAid_Rebinder_RequestRebind)
method, or by implementing the
[IRebindingTrigger](https://artehacker.com/UITKEditorAid/v1/api/ArteHacker.UITKEditorAid.IRebindingTrigger.html) interface
in your elements, if you are up for more advanced stuff.

### [ValueTracker](https://artehacker.com/UITKEditorAid/v1/api/ArteHacker.UITKEditorAid.ValueTracker-1.html)
Some times you don't need a VisualElement for anything visual, sometimes you just need a quick way to get a callback when
a property changes. This element helps you with that.

### [ListOfInspectors](https://artehacker.com/UITKEditorAid/v1/api/ArteHacker.UITKEditorAid.ListOfInspectors.html)
A list that is similar to the component's list in GameObjects. I use it with ScriptableObject subassets. You still have to
do the stuff that's not related to UIToolkit yourself, like handling assets and the lifetime of your objects, which is
outside the scope of this package; but if you know how to do that, this could be very helpful.

![ListOfInspectors preview](doc_images/ListOfInspectors.png)
<br/>

### [More Stuff](https://artehacker.com/UITKEditorAid/v1/api/ArteHacker.UITKEditorAid.html)
Click the link to go to the docs homepage. There is stuff to replace some common IMGUI methods, like a 
[Disabler](https://artehacker.com/UITKEditorAid/v1/api/ArteHacker.UITKEditorAid.Disabler.html) element that's equivalent
to `EditorGUI.DisabledScope`, and `FixedSpace`/`FlexibleSpace` elements equivalent to GUILayout's `Space` and `FlexibleSpace`.
There are also some extension methods that I've found useful for UIToolkit editor development, and some UITK manipulators.
<br/><br/>

## A caveat
Currently, all elements that need rebinding  (i.e. Lists and ManagedReferenceFields), don't support UXML. That's because
they need to get a `SerializedProperty` or a `SerializedObject` in their constructor. The correct way to solve that would
be to obtain them with Unity's binding system, but the required API is not public yet. I don't want to use reflection
because that API seems likely to change, and editors that break when doing a minor Unity update are awful.<br/><br/>

## IMPORTANT: Avoid collisions when including this code inside packages and plugins
Sometimes one needs to put a copy of a library inside a Unity plugin; that's very ok, that's what the MIT license is for.
If you do that, please take steps to avoid collisions when your users have also installed this package themselves. The
easiest way to do it is:

1. Rename the ArteHacker part of the namespace. Most text editors have an automated way of doing that.

2. Delete the .asmdef file inside the Editor folder. Create a new one if you need it; renaming it is not enough because
some users reference them as assets (with their GUID).