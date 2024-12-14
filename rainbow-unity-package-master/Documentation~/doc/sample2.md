# Install extra dependencies:
Select `Window/Package Manager` in the menu bar.

Check Package Manager window, Click `+` button.

![Install Package Manager from menu bar](../images/install01.png)

Select `Add package from git URL` and enter `com.unity.vectorgraphics`

![Import com.unity.vectorggraphics](../images/sample02dep1.png)

Select `Add package from git URL` and enter `https://github.com/kirevdokimov/Unity-UI-Rounded-Corners.git`

![Import unity rounded corners](../images/sample02dep2.png)


# Import the Mini Rainbow Client Sample:
From the `package manager`, click on the `Rainbow SDK Unity WebRTC` and unfold the `Samples`.
Import `Common`, and `Mini Rainbow Client`.

![Common and Basic Rainbow Sample](../images/sample02import.png)

# Configure and start Mini Rainbow Client Sample:

From your `project` view unfold the `Mini Rainbow Client` go to `Scenes` drawer and double click on the `RainbowClientScene` scene.

![Open sample scene](../images/sample0201.png)
 
If TextMesh Pro is not already in use in your project you will be prompted to import the TMP. 
Click on the first button `Import TMP Essentials`, then close the window.

![Import TMP Essentials](../images/sample02.png)

In the `hierarchy` view, under `Canvas` unfold the `Rainbow` tree, select the `RainbowController` game object, and in the inspector specify the rainbow related parameters:
+ `APP_ID` and `APP_SECRET_KEY`
Those are the application credentials you received when you created your rainbow application from the [Rainbow Developer Site](https://developers.openrainbow.com).

+ `HOSTNAME`
This is the rainbow platform on which your application and user was created.
If you are using the sandbox, use `sandbox.openrainbow.com`, else use `openrainbow.com`.

+ `LOGIN_USER1` and `PASSWORD_USER1`
Those are the credentials of the user.

![Open sample scene](../images/sample0102.png)

Enter Play mode. You should see a simple ui containing 3 dropdown:

![Open sample scene](../images/sample04a.png)

And after a short while, the sdk should be connected to rainbow, and two additional drop downs and buttons will show up.

![Specify Rainbow Settings](../images/sample04b.png)

The first row contains three dropdown in which you can specify which device the sdk will use 
+ as a microphone.
+ as a webcam when required to publish on the 'Video' channel. It can be either a unity camera, or a physical device web cam.
+ as a video stream when required to publish on the 'Sharing' channel. It can be either a unity camera, or a physical device web cam.
If you intend to publish on both sharing and camera channel, the `video camera` and `sharing camera` must be different.

![Specifying rainbow devices](../images/sample04c.png)

It can also be convenient to use an audio clip as a `Microphone device`.
To do so, before entering Play mode, unfold `Canvas/Settings` and in the component `SettingsUI` specify a reference to this Audio Clip in the parameter `Audio Clip`.
Afterwards, the `Microphone Device` drop down will contain an option to use your clip.

![Specifying an audio clip](../images/sample0104.png)

Now that the SDK is connected to rainbow, it is possible to issue a  direct (P2P) call to one of your contacts, or to join an active conference.
If your user doesn't have contacts or is not part of a conference yet, use another user and the rainbow client to invite him as a contact and invite him in some bubble,
then use the rainbow client with your user's credentials to accept the invitations.

To issue a P2P call to a contact, select the desired contact in the `Contacts` dropdown, and click on `P2P Call`.

![Call a P2P Contact](../images/sample05.png)

To join an active conference, select the conference in the dropdown, and click on `Join Conference`.
If no one has started the conference, no conference will show in this list.

![Join a conference](../images/sample06.png)

Once the call is established, the `Join conference` and `P2P Call` rows will be hidden, and the ui displayed looks like the rainbow client.

![In call features](../images/miniclientinconf.png)


| |
| ----------- |  
|[Back to `Index`](../index.md)|
|[Back to `Install`](install.md)|
|[Go to `Develop with Rainbow WebRTC`](developing_general.md)|
|[Go to `Rainbow WebRTC Unity Specifics`](developing_unity.md)|
