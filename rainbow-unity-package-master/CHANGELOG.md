# Change Log

All notable changes to this project will be documented in this file.

### [2.6.16.1] - LTS - 2023-06-16
---

## Fix

- `TaskCanceledException` with inner exception `TimeoutException` was mishandled in `HttpClient`
- Store `Channel` objects in internal cache even if found using search APIs
- JSON parsing was not done correctly on null properties. As a result, parsing `DateTime` objects with a null property returned `DateTime.UTCNow` instead of `DateTime.MinValue`
- Wrong URI used to access media pillar information
- Fix bad race condition which didn't allow to use LoginToEventPipe() just after LoginWithoutEventPipe()
- Fix ConferenceGetFullSnapshot: participants were not well managed 

### [2.6.16] - LTS - 2023-05-26
---

## Fix

- XMPP Iq messages (set as result only) were not released from memory once managed.
- According some race condition, Bubble/Conference could not be joined.
- Fix crash if IniFileParser could not be created (incorrect path specified and the default one can be used too)
- WebRTC context: Ice Candidate and senders property were badly set in Jingle Messages.

## Enhancements

- **SDK now supports AOT** (Ahead Of Time) Compilation and is compatible with **Unity framework** and its **IL2CPP** (Intermediate Language To C++) scripting backend
    - All codes about REST and JSON have been reworked in consequence.
    - It was also necessary to change some properties name to ensure correct JSON parsing.

- **Static SDK errors list:**
    - Now if the SDK itself raised an error,  a specific **ErrorCode** and **ErrorDetailsCode** are used. See `SdkInternalError enum` for a full list.

- **Auto-Reconnection service:**
    - Handles correctly **429 - Too Many Requests** and **503 - Service Unavailable** returned by the server. 
    - This service will stop **ONLY** when a **"401 - Unauthorized"** is returned by the server when **Login** and/or **Login With Token** REST Requests are used.
    - Smooth reconnection (i.e. the cache is not always deleted) according the disconnection context.
    - The counter "current nb attempts" is now set to zero only once the authentication process AND the initialization process are both done succesfully.
    - It's now possible to add a random delay between each attempts using **MaxRandomDelayBetwenAttemps**.
    - Add Login method (so it's now possible to directly use this service even for the first login) - see **AutoReconnection.Login()** and  **AutoReconnection.LoginUsingPreviousCredentials()** methods
    - **This service will no more be optional in next release.**

- **Proxy**
    - Before only an Url with login/pwd (optional) could be set. Now a **WebProxy** object can be set allowing more options.

- **Restrict use of HttpClient:**
    - Use only one HttpClient object to access Rainbow Server to increase performance (Several requests can still be performed in same time). 
    - If necessary another one is available to access any other server - see **Application.GetHttpClient()** and **Application.GetSecondaryHttpClient()**.
    - Both are using the same **WebProxy** settings.

- **JSON Parser**
    - A new JSON Parser is used using **SimpleJSON** library now embedded in the SDK which replaces `Newtonsoft.Json`.
    - **UtilJSON** class has also been added to facilitate its usage. 
    - Read a new dedicated guide available in the online documentation web site if you need to use also a JSON parser. 

- **Randomization** process is now centralised and use only one **Random** object.

## Framework targets
- .Net Standardd 2.0 and 2.1 are still supported.
- .Net Core App 3.1 will be removed in the next release (already deprecated by Microsoft).
- .Net 5.0 has been removed (already deprecated by Microsoft).
- .Net 6.0 has been added.

## Dependencies
- Dependencies no more used: `Newtonsoft.Json`, `RestSharp` and `System.IdentityModel.Tokens.Jwt`
    - This libraries (with their own dependencies) was huge with a lot of features but not all really used. And it was necessary to folllow security warnings and new versions.

- Dependency added and directly embedded in the SDK: `SimpleJson` (MIT License)
    - It's a very smart library which does greatly its job. It's AOT compatible and very stable.

- Dependency `Sharp.Ws.Xmpp` now uses `Microsoft.Extensions.Logging.Abstractions` `v7.0.0` (before v6.0.0 was used) 

## API in details
- **AutoReconnection** service:
   - `DelayBetweenAttempts`: **updated** Default values are now { 200, 400, 800, 1600, 3200, 6400, ... }
   - `MaxRandomDelayBetwenAttemps`: **new** Max random delay in milliseconds to use beetween each attempt. Default values: { 100, 200, 400, 800, 1600, 3200, ... }
   - `RandomDelayBetweenAttempts`: **new** Random delay in milliseconds to use beetween each attempt.
   - `GenerateRandomDelayBetweenAttempts()`: **new** To generates new values in **RandomDelayBetweenAttempts** using values set in **MaxRandomDelayBetwenAttemps**
   - `Login()`: **new** Start this service and try to log on the server using specified credentials.
   - `IsOneNetworkUp()`: **updated** Fix type error in the name of this method - before it was IsOneNet**W**orkUp
    
- **Restrictions** object:
    - `LogRestRequestOnError`: **updated** To log all Rest Requests on error only - **True** by default. Before it's was wrongly called **LogSdkResultOnError**
    - `LogRestRequest`: **new** To log all Rest Request - **False** by default.
    
- **HttpRequestDescriptor** object: **new** To describe an Http Request
    - `HttpRequestDescriptor`: **new** Constructor with URI, Method (GET, POST, ...), request content (string or byte[]) , request content type
    - `AddQueryParameter`: **new** To add a parameter in the query
    - `AddHeader`, `AddHeaders`: **new** To add one or several headers
    - `AddAcceptContentType`, `AddAcceptContentTypes`: **new** To accept one or several Content Types as valid in the response
    - `SetAuthenticationHeaderValue`: **new** To set an authentication header value (for example to perform a Basic Authentication)
    
- **HttpClient** object: **new** To perform HTTP request **HttpRequestDescriptor**
    - `RequestAsByteArrayAsync`: **new** Perform an async HTTP Request with a byte[] expected as response
    - `RequestAsStringAsync`: **new** Perform an async HTTP Request with a string expected as response
    
- **SimpleJSON** namespace:
    - **new** JSON Parse - check the online documentation and the specific guide
           
- **SDKError** object:
    - `ResponseContent`: **new** Response Content of the REST Request
    - `ResponseContentType`: **new** Response Content Type of the REST Request
    - `ToString()`: **updated** Add Content Type and Http Status Code (as int)
    
- **Util** class:
    - `RandomNext`: **new** Returns a random integer that is within a specified range
    - `WriteToJsonFile`: **removed**
    - `ReadFromJsonFile()`: **removed**
    - `SerializeSdkError()`: **removed** Can be replaced by `callback.Result.ToString()`
    - `SerializeSdkResult()`: **removed** Can be replaced by `callback.Result.ToString()`
    - `SerializeFromResponse()`: **removed** Can be replaced by `callback.Result.ToString()`

### [2.6.15] - LTS - 2023-02-10
---

## Fix

- Using XMPP event mode, Calendar presence was not correctly taken into account

- Webinar were badly managed as Conference. It's no more the case. (Webinar are not managed yet)
 
- Server can return error in REST API calls without ErrorCode, ErrorMsg, ErrorDetails and/or ErrorDetailsCode. When it's the case, the parsing was failing

- GetConferencesInProgress: its callback will be raised only once info about conferences are fetched (if any)

- GetRosterContactsCacheList could failed if the contact list is changing when this method is called 

## Enhancements

- BasicLoggerFactory

A new class **BasicLoggerFactory** permits to easily manage logging features without the need of any third party (like NLog or Log4Net for example)

It's a very basic one since an event is raised each time a log entry is added but can be very usefull.

- Delete all messages in a Bubble

It's now possible to delete all messages in a Bubble conversation. (it was already possible for a P2P conversation)

Messages are deleted only for the current user. They are NOT deleted for remote(s) user(s).
 
## API in details

- **BasicLoggerFactory** object: **added** A basic logger factory that raise events each time a log entry is added.

- **Bubble** object:

    - `Webinar`: **added** Object not null if this Bubble is to host a webinar

- **Call** object:

    - `IsActive`: **added** To know if the call is Active - Media can be added/removed only on an active call

- **Conversation** object:

    - `IsAlertNotificationEnabled`: **removed**
    
    - `SubType`: **added** Equals "webinar" to indicate if the purpose of this conversation is to host a webinar

- **InstantMessaging** service:

    - `DeleteAllMessages`: **updated** It's now possible to use the cnoversation Id of a Bubble to delete all messages for the current userT. hey are NOT deleted for remote(s) user(s).
    
    - `SendAckMessage`, `AnswerToAckMessage`: **updated** new parameter msDelay: Delay (in ms) before to use the callback with an error if the Peed didn't answered yet
�   
    - `SendAdHocCommand`, `AnwserToAdHocCommand`: **updated** new parameter msDelay: Delay (in ms) before to use the callback with an error if the Peed didn't answered yet

### [2.6.14] - LTS - 2023-01-20
---

## Enhancements

- User settings and synchronization status with a provider (Office 365, Goggle Calendar, Teams)
    - **Contacts** service now permits to manage user settings and to know the synchronization status with a provider
    
    - Associated events are avaialble to be inform of any updates done on antoher device for the current user. 
    
    - User settings are automatically get from server in the login process 

- Presence and Aggregated Presence using Teams and/or Calendar (OFfice 365 or Google) 
    
    - Teams Presence is now taken into account if user has activated it (internal reference RQRAINB-8649). Calendar Presence has also been fixed.   
    
    - To support this a rework was necessary:  **Presence** object has new properties/methods. **PresenceInfo** and **PresenceCalendar** have been removed
    
    - It permits to build a correct Aggregated Presence when **XMPP event mode** is used. Due to current server restriction, using **S2S event mode** or **REST API** only don't permit to build a correct Aggregated Presence.
     
- AutoReconnection server and stream error returned by the server.

    - Now the AutoReconnection service is no more stopped and the Web Sockect is not more closed each time a stream error is returned by the server.
    
    - Accodring the stream error a criticity taken is set - see **ConnectionStateEventArgs** object and **AutoReconnection** service for more details
    
- Simplification in **Conferences** service:

    - **MediaPublication** is used instead of **Conference.Publisher** and **Conference.PublisherMedia** which have has been removed
    
    - **ConferencePublishersEventArgs** has been removed. Use instead **MediaPublicationsEventArgs**
    
    - **ConferenceGetPublishersFromCache** has been removed. Use instead **ConferenceGetMediaPublicationsFromCache**
    
    - All this permits to reduce code in **Conferences** service:   

## API in details

- **Call** object:
    - `PeerId`: **added** Id of the peer (not null in P2P context only)
    
    - `PeerJid`: **added** Jid of the peer (not null in P2P context only)
    
- **CalendarState** object:
    - `IsEnabled`: **added** True if calendar status sharing is enabled or not

- **ConnectionStateEventArgs** object:
    - `Criticity`: **added** Criticity ofthe stream error: 'fatal', 'warn' or 'error'

- **Conferences** service:
    - `ConferenceMediaPublicationsUpdated`: **renamed** previously named **ConferencePublishersUpdated**
    
    - `ConferenceGetMediaPublicationsFromCache`: **renamed** previously named **ConferenceGetPublishersFromCache**

    - `ConferenceIsParticipantUsingJid`: **added** To check if the user jid specified is particiant according the list provided

- **Conference.Publisher** object: **removed** data are now in **MediaPublication** object

- **Conference.PublisherMedia** object: **removed** data are now in **MediaPublication** object

- **Contacts** service:
    - `SynchroProviderStatusChanged`: **added** Event raised when synchronization status with a provider has changed.
    
    - `UserSettingsChanged`: **added** Event raised when at least one user settings has changed.
    
    - `GetUserSettingBooleanValue`: **added** To get the value (as boolean) of the user setting specified
    
    - `GetUserSettingStringValue`: **added** To get the value (as string) of the user setting specified
    
    - `GetUserSettings`: **added** To get all user settings (name and value)
    
    - `UpdateUserSetting`: **added** To update, on server side and locally, the value of a user setting
    
    - `GetSynchroProviderStatusList`: **added** Get the list of synchronisation status stored
    
    - `GetTeamsPresenceStateFromCurrentContact`: **added** To get teams presence state of the current contact.
    
    - `GetTeamsPresenceStateFromContactId`: **added** Get teams presence  of the specified contact id
    
    - `GetTeamsPresenceStatesFromContactsId`: **added** et teams presence states of the specified list of contact id
    
    - `CreatePresence`: **added** Create a **Presence* object using Jid and Resource of the current user and the presence level and presence details specified

- **MediaPublication** object: (previously named **ConferenceMediaPublication**)
    - `CallId`: **renamed** previously named **ConferenceId**
    
    - `PublisherJid_im`: **added**  Jid_im of the publisher
    
    - `Simulcast`: **added**  To indicate if this media is sent using simulcast

- **Presence** object:  
    - `Apply`: **added** True, if this presence must be used to build the aggregated presence.
    
    - `BasicNodeJid`: **added** The basic jid Node of the contact
    
    - `Date`: **added** The Date when this presence has been set
    
    - `Until`: **added** The validity date until this presence is valid (if not equals to DateTime.MinValue)
    
    - `PresencePhoneState`: **added** The presence state of the phone (if any)
    
    - `PresenceCalendar`: **added** (static method) Facilitator to create Presence for Calendar purpose
    
    - `PresencePhone`: **added** (static method) Facilitator to create Presence for Phone purpose
    
- **PresenceInfo** object: **removed** data are now in **Presence** object

- **PresenceCalendar** object: **removed** data are now in **Presence** object

- **PresencePhone** object: **renamed** in **PresencePhoneState**

### [2.6.13] - LTS - 2022-12-07
---

## Fix

- **Conferences** service:

    - `GetConferencesInProgress` now correctly managed server answer if several conferences are in progress.
    
    - `GetConversationByIdFromCache`, `GetConversationByPeerIdFromCache`, `GetConversationByJidFromCache`, `GetConversationIdByPeerIdFromCache` and `GetConversationIdByJidFromCache` now check if parameter specfied is null or empty 

- **Contacts** service:

    - `SetBusyPresenceAccordingMedias` and `RollbackPresenceSavedFromCurrentContact` method set a bad priority to manage presence level
    
    - `EnableCalendar` used a bad HTTP Method: PATCH instead of PUT

- **InstantMessaging** service: Ensure to encode string in XML when alternat content is used to send messages

- **Jingle** service: `SendIceCandidate` method was incorrect. An attribute was badly set. This permits to resolve NAT traversal trouble in WebRTC (occurred mainly in P2P context)

## Enhancements

- Documentation enhancement about **password length which should be at least 12**. It's not yet mandatory but it'll be soon so it must be taken into account quickly.

- **New HTTP Header** (`x-rainbow-client-id`) has been added in each REST request to improve support on server side

- **CallParticipant** object: `FromContact` returns a valid object if a Phonenumber is not null but Contact object is.

- **Contacts** service:

    - `RegisterCalendar`: **new** To register to a third party calendar (goggle or office365 for example)
    
    - `UnregisterCalendar`: **new** To unregister to a third party calendar.

- **CalendarOutOfOfficeState** object:

    - `Busy`: **removed** Not used
    
    - `Provider`: **new** The third party calendar used 

### [2.6.12] - LTS - 2022-10-31
---

## Fix

- Avoid throwing exceptions when there is server congestion


### [2.6.11] - LTS - 2022-10-05
---

## Important Notice

- Editing or deleting messages has changed on server side. Nothing needs to be changed in terms of SDK API calls. However the property `Restrictions.UseMessageEditionAndDeletionV2` is now deprecated. Updating its value has no consequence since `MessageEditionAndDeletionV2` is now always used.

## Update

- Enriched documentation


### [2.6.10] - LTS - 2022-09-14
---

## Important Notice

**Conferences service has been fully remorked**
 - It's `mandatory` to upgrade to this new SDK version if you are using these features. 
 - Personal Conferences (i.e. PGI) have been removed
 - Old code about API Conferences V1 have been removed

## Fixes

### Telephony service:

No event was raised when a call is transfered. Now a new Call is created and an event CallUpdated is raised (internal ref. CRRAINB-28750).

### WebSocket connexion

In very rare case, an unhandled exception could be raised if the web socket cannot be opened.

### [2.6.9] - LTS - 2022-08-23
---

## Important Notice

 - Due to server update, only `Conference API V2` is now supported whatever the value set to `Restrictions.UseAPIConferenceV2` which is now obsolete.
 
 - Editing or deleting messages will change in future releases due to server updates. Nothing needs to be changed in terms of SDK API calls. However a new property `Restrictions.UseMessageEditionAndDeletionV2` allows to switch to this new version (false by default today). When the new version will be available, this property will be deprecated.  
 
## Fixes

### AutoReconnection service:

If the server asked for the disconnection, this service is now automatically stopped and event `Cancelled` is raised with `DisconnectedByServer` as value. It's possible to have more details using `GetServerDisconnectionInformation` method. (internal reference: RQRAINB-7605)

This can occur if the same user has more than 5 active connections to the server using XMPP event mode. In this case you will have this information: 
 - Reason: `resource-constraint` 
 - Details `Max sessions reached`

### Bubbles service:

Method `GenerateNewPublicUrl` and `CreatePublicUrl` no more compute the URL but use the one specified by the server (internal reference: RQRAINB-7235)

### Instant Messaging service:

Method `EditMessage` didn't reset alternative contents (if any) of the message updated.

### WebSocket connexion

In rare case, no event could be raised if the web socket cannot be opened on first attempt.

## Enhancements

### Restrictions:

It's now possible to set the size of chunck when a file is uploaded or downloaded using this properties:
 - `ChunkSizeUpload`: Size (in bytes) of the chunck used when a file is uploaded - Default value 1048576 (1024 * 1024 bytes => 1 Mb)
 - `MaxChunkSizeUpload`: Maximum size (in bytes) of the chunck authorized by the server when a file is uploaded (read only)
 - `ChunkSizeDownload`: Size (in bytes) of the chunck used when a file is downloaded - Default value 5242880 (1024 * 1024  * 5 bytes => 5 Mb)
 - `MaxChunkSizeDownload`: Maximum size (in bytes) of the chunck authorized by the server when a file is downloaded (read only)

### Application service:

It's now possible to set specific security protocol used in the SDK once `Application` service has been created using `SetSecurityProtocol` method. If this method is not used, the same defaults are used as in previous versions of the SDK. They are set like this:
 
```cs
#if NETCOREAPP || NET5_0_OR_GREATER
    // USE TLS 1.2 or TLS 1.3 only
    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;
#elif NETSTANDARD
    // USE TLS 1.2 only
    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
#endif
```

Event `ConnectionStateChanged` provide more information about the disconnection if it's the server which asked it.

**It's now possible to log on different files if several instance of the SDK is used in sametime** (for example if you use several bots in the same process). You need to set to the new parameter `loggerPrefix` a different value for each instance when `Rainbow.Application` object is created. You also need to use a different configuration file for your logger. See the online documentation for more details.    

### AutoReconnection service:

The signature of the `Cancelled event` has been changed to make it easier to know the reason of the trigger: `Cancelled, DisconnectedByServer, InvalidCredentials, Logout, MaxNbAttemptsReached or TokenExpired`.

### Bubbles service:

The method `IsMember` has been renamed to `IsAccepted` (because a user can be a member but has not yet accepted the invitation and this method check the status Accepted).

Several methods accepts now an optional parameter to specify a userId (null by default - so to check current user): `IsAccepted, IsArchived, IsModerator`.

New methods added: `IsInvited, IsCreator, IsGuest, GetMemberOfBubble`.

### InstantMessaging service:

A new `EditMessage` method (with more parameters) permits to update a previous message with `a new body` and `with new alternative contents`.

### Telephony service:

New methods `MakeCall, MakeCallWithSubject and MakeCallToMevo` with resource as parameter to allow only one device (the one with using this resource) or all devices to accept this call (if no resource is specified) (internal reference: RQRAINB-4732)

### [2.6.8] - LTS - 2022-07-01
---

### Enhancement

**Contacts service:**

`SearchContactsByDisplayName`: if company Id is not specified, the search is performed in the company of the current user and outside of its company (so internally 2 requests to the server are done instead of one)


### [2.6.7] - LTS - 2022-06-15
---

### Fix
**Conferences service:**
 - `ConferenceSubscribeToParticipantVideoStream`: Bad parameter used to set SubStreamLevel

**InstantMessaging service:**
 - Remove internal tests which didn't permit to send file in **S2S event mode**.
 - Some presence level couldn't be set correclty in **S2S event mode**.

### Reorganization

- **MessageType** object is now in **Rainbow.Model** namespace. Before it was in **Rainbow.Model.AckMessage** namespace. It impacts slightly methods using **AckMessage** object

### API update in details:

**InstantMessaging service:**
 - `AdHocCommandReceived`: **new** Event raised when the current user received an **AdHocCommand**
 - `AnwserToAdHocCommand`: **new** To answer to an **AdHocCommand** received.
 - `SendAdHocCommand`: **new** To send an **AdHocCommand** to a specific user resource (i.e. a specific device used by a user and not all of these devices).

**Conferences service:**
 - `ConferenceSharingTransfertStatusUpdated`: Event raised when the sharing transfer status has changed in a Conference. (based internally on specific **AdHocCommand** received)
 - `ConferenceSendSharingTransfertStatus`: To send sharing transfert status i.e. send a request or accept/refuse a sharing transfer. (based internally on specific **AdHocCommand**)
 - `ConferenceGetSharingPublisherDetails`: To get Jid and Resource of the sharing publisher in the specified Conference.
 
### [2.6.6] - LTS - 2022-05-23
---

### Fix

**Contacts service:**
 - `SearchContactsByDisplayName`,  `SearchContactByPhoneNumber` and `Contacts.SearchContactsByTag`: CompanyId parameter was badly used to restrict the search

### API update in details:

**Call object**

 - `ConferenceId`: **new** To know the Conference Id if this object describes a communication in a Conference context
 - `ConferenceJid`: **new** To know the Conference Jid if this Call object describes a communication in a Conference context
 - `IsRemoteVideoMuted`: **removed**  

**Call.Type enum**

 - `WEBRTC_P2P`: **renamed** Before it was `WEBRTC`  
 - `WEBRTC_CONFERENCE`: **renamed** Before it was `CONFERENCE`
 
**Publisher object**

 - `Medias`: **renamed and updated** Before it was `Media` and now it's a list of `PublisherMedia` objects (before it was a list of String)   
 
**PublisherMedia object**  `new`
 
 - `Media`: **new** Media used by the publisher - can be "sharing" or "video"
 - `Simulcast`: **new** To indicate if this media is sent using simulcast or not
 
**Jingle service:**

 - `Jingle_ConferenceSessionInitiate`: **new** Event raised when a new session initiate has been received in a conference context (i.e. a publisher has started a video or sharing stream)
 - `CreateJingleSessionForConference`: **new** To create a conference session in the Jingle service
 - `SendJingleSessionMessage`: **updated** Add new optional parameter "localMedias"
 
**Conferences service:**

 - `ConferenceOwnerUpdated`: **new** Event raised when the owner of the conference has been updated
 - `ConferenceJoin`: **updated** Callback now returns the Conferecen Jid on success (before it was True)
 


### [2.6.5] - LTS - 2022-05-04
---

### Fix

- **AutoReconnection**: Prevent exception when trying to postpone internal Cancelable Delay object which could be null.
- **Conferences**: Even if an admin user is connected, get only Conference End Points of the current user in login process.

### [2.6.4] - LTS - 2022-04-07
---

### AckMessage

**New kind of message (with ack)** which permits to send information to a **specific ressource/device** of a user (and not to all ressources/devices of a user).

Using these messages **does not interact with current IM features** in standard Rainbow Applications (i.e UCaaS application): they are not seen and not stored in the message history.

When sending an AckMessage you can specify an **action**, a **mime type** and a **content** (using a String or a XmlElement).

On Reception, you can answer using them kind of parameters.

To know the list of ressources/devices of one user, you can use **Contacts.GetPresencesFromContactId** method which provides presences list of the specified contact for all his ressources/devices.

### Fix
- **Channels**: **GetDetailedAppreciations** could not retunrs all Appreciations
- **FileStorage**: **CreateFileDescriptor** and **UploadFile** now returns an error if they are used in Channel context. **Channels.UpdateAvatarFromChannelId** method must be used instead to update Channel Avatar. 
- **SdkError**: **SdkError.FromSdkError** method didn't copy **Resource** and **Method** properties 

### API update in details:

**InstantMessaging service:** 

 - `AckMessageReceived`: **new** Event raised when an AckMessage has been received
 - `SendAckMessage`: **new** 
 - `AnswerToAckMessage`: **new**  
 
**AckMessage object:**
 - `Id`: **new**
 - `Type`: **new**
 - `ToJid`: **new**
 - `ToResource`: **new**
 - `FromJid`: **new**
 - `FromResource`: **new**
 - `Action`: **new**
 - `MimeType`: **new**
 - `Content`: **new**
 - `XmlContent`: **new**

**FileStorage service:** 

 - `CreateFileDescriptor`: **fix** Return an error if used in Channel context
 - `UploadFile`: **fix** Return an error if used in Channel context 

**Channels service:** 

 - `GetDetailedAppreciations`: **fix** Could not return all appreciations


### [2.6.3] - LTS - 2022-03-16
---

### Conference API V2

In this version, Conference API V2 (for WebRTC conference) is supported. A flag (see **Restrictions.UseAPIConferenceV2** - to set before any login process) permits to use V1 (by default) or V2. At term, only the V2 will be used/available. 

This doesn't affect Personal Conference (i.e. Conference using PSTN devices)

Since Conferences features have increased, there are now centralized in **Conferences** service (accessible using **Application.GetConferences()**). Before they were avaialble in **Bubbles** service.

**NOTE**: the flag **Restrictions.UseConferences** (true by default) permits to win some time in login process (if set to false) if you don't need to use these features.   

### Logging enhancement

A new flag (see **Restrictions.LogSdkResultOnError**) set to true by default allows to log any error occuring when a REST API is used. 

Before it was necessary, for the developper using the SDK, to log them individually. To improve analysis, it's now automatic (at least if log functionality are used) if this flag is set to true.

### Directory Contact

It's now possible to manage **Personal Directory Contact** or **Company Directoy Contact**. 

**DirectoryContact** object has also more properties than before.

### Fix

 - In S2S Context, IM message could not be sent.
 - ChannelInfoUpdated event could be raised instead of then ChannelCreated event.

### API update in details:

**Bubbles service:**

 - **All events / methods** linked to Conference features have moved to **Conferences** service

**Conferences service:** 

 - `ConferenceParticipantsUpdated`: **new** Event raised when at least one Participant has been add/removed/updated in a Conference 
 - `ConferencePublishersUpdated`: **new** Event raised when at least one Publisher has been add/removed/updated in a Conference
 - `ConferenceTalkersUpdated`: **new** Event raised when at least one Tlaker has been add/removed/updated in a Conference
 - `ConferenceRejected`: **new** Event raised when a Conference has been rejected by the current on another device
 - `ConferenceRemoved`: **new** Event raised when a Conference removed (i.e. finished)
 - `ConferenceUpdated`: **new** Event raised when a Conference has been updated (mute, lock, ...)
 - `GetBubbleByConferenceIdFromCache`: **new** Get bubble for the specified Conference Id
 - `GetBubbleIdByConferenceIdFromCache`: **new** Get bubble Id for the specified  Id
 - `GetConferenceIdByBubbleIdFromCache`: **new** Get Conference Id for the specified bubble Id
 - `GetConferencesInProgress`: **new** Get Conferences in progress
 - `ConferenceAllowed()`: **new**  To know if Conference feature is allowed
 - `ConferenceGetByIdFromCache`: **new** Get Conference using its Id from the cache
 - `ConferenceGetSnapshot`: **new** Get Conference info from server
 - `ConferenceGetFullSnapshot`: **new** Get full Conference info from server (Participants, publisher, ...)
 - `ConferenceGetListFromCache`: **new** Get list of conference ni progress from the cache
 - `ConferenceGetPublicUrl`: **new** Get Conference meeting URL
 - `ConferenceGenerateNewPublicUrl`: **new** Generate new Conference meeting URL
 - `ConferenceJoin`: **new** Join the conference
 - `ConferenceLockOrUnlocked`: **new** Lock/unlock Conference
 - `ConferenceMuteOrUnmute`: **new** Mute/Unmute Conference
 - `ConferenceGetParticipantParameters`: **new** Get participants parameters for the conference
 - `ConferenceSetParticipantParameters`: **new** Set participants parameters for the conference
 - `ConferenceGetParticipantsFromCache`: **new** Get participants in the conference from the cache
 - `ConferenceGetPublishersFromCache`: **new** Get publishers in the conference from the cache
 - `ConferenceGetTalkersFromCache`: **new** Get talkers in the conference from the cache
 - `ConferenceDelegate`: **new** Delegate Conference
 - `ConferenceAddPstnParticipant`: **new** Add PSTN Participant to Conference (i.e. a Rainbow Hub user)
 - `ConferenceDropPstnParticipant`: **new** Drop PSTN Participant to Conference (i.e. a Rainbow Hub user)
 - `ConferenceMuteOrUnmutePstnParticipant`: **new** Mute/Unmute a PSTN participant
 - `ConferenceDropParticipant`: **new** Drop a participant
 - `ConferenceMuteOrUnmuteParticipant`: **new** Mute/Unmute a participant
 - `ConferenceRecordingStart`: **new** Start Conference recording
 - `ConferenceRecordingStop`: **new** Stop Conference recording
 - `ConferenceRecordingPause`: **new** Pause Conference recording
 - `ConferenceRecordingResume`: **new** Resume Conference recording
 - `ConferenceReject`: **new** Reject Conference
 - `ConferenceStart`: **new** Start Conference 
 - `ConferenceStop`: **new** Stop Conference
 - `ConferenceSubscribeToParticipantVideoStream`: **new** Subscribe to Participant Video Stream 
 - `ConferenceTalkingTime`: **new** Get Talking time of Participants
 - `PersonalConferenceAllowed`: **new** To know if Parsonal Conference feature is allowed
 - `PersonalConferenceGetBubbleFromCache`: **new** Get bubble for the personal Conference from cache
 - `PersonalConferenceGetBubbleIdFromCache`: **new** Get bubble Id for the personal Conference from cache
 - `PersonalConferenceGetId`: **new** Get Id of the Personal Conference
 - `PersonalConferenceGetFromCache`: **new** Get Personal Conference from cache
 - `PersonalConferenceGetPassCodes`: **new** Get pass codes
 - `PersonalConferenceResetPassCodes`: **new** Resest and get new pass codes
 - `PersonalConferenceGetPhoneNumbers`: **new** Get phone numbers to reach the Personal Conference
 - `PersonalConferenceGetPublicUrl`: **new** Get personal Conference meeting URL
 - `PersonalConferenceGenerateNewPublicUrl`: **new** Generate new personal Conference meeting URL
 - `PersonalConferenceGetSnapshot`: **new** Get personal Conference info from server
 - `PersonalConferenceGetFullSnapshot`: **new** Get full personal Conference info from server (Participants, publisher, ...)
 - `PersonalConferenceDropParticipant`: **new** Drop a participant
 - `PersonalConferenceGetParticipantParameters`: **new** Get participants parameters for the personal conference
 - `PersonalConferenceSetParticipantParameters`: **new** Set participants parameters for the personal conference
 - `PersonalConferenceGetParticipantsFromCache`: **new** Get participants in the personal conference from the cache
 - `PersonalConferenceGetPublishersFromCache`: **new** Get publishers in the personal conference from the cache
 - `PersonalConferenceGetTalkersFromCache`: **new** Get talkers in the personal conference from the cache
 - `PersonalConferenceJoin`: **new** Join the personal conference 
 - `PersonalConferenceLockOrUnlock`: **new** Lock/Unlokc the personal conference
 - `PersonalConferenceMuteOrUnmute`: **new** Mute/Unmute the personal conference
 - `PersonalConferenceMuteOrUnmuteParticipant`: **new** Mute/Unmute a participant in the personal conference
 - `PersonalConferenceStart`: **new** Start the personal conference
 - `PersonalConferenceStop`: **new** Stop the personal conference
 
 
**DirectoryContact object:**

 - `UserId`: **new** Id of the user 
 - `Type`: **new** Type of the directory entry: "user" or "company"
 - `CompanyName`: **new**
 - `Department`: **new**
 - `Street`: **new**
 - `City`: **new**
 - `State`: **new**
 - `PostalCode`: **new**
 - `Country`: **new**
 - `OtherPhoneNumbers`: **new** List of 'other' phone numbers
 - `Tags`: **new** List of tags
 - `Custom1`: **new**
 - `Custom2`: **new**
 
**Contacts service:** 

 - `GetPresencesFromUser`: **new** Get presences list of the current contact (from all resources) asking the server (not using local cache) - Useful if you have set EventMode = NONE (i.e. no Xmpp or no S2S)
 - `CreateDirectoryContact`: **new** Create Personal Directory Contact
 - `UpdateDirectoryContact`: **new** Update Personal Directory Contact
 - `DeleteDirectoryContact`: **new** Delete Personal Directory Contact
 - `GetDirectoryContact`: **new** Get Personal Directory Contact
 - `GetDirectoryContactsList`: **new** Get list of Personal Directory Contact

**Administration service:** 

 - `CreateDirectoryContact`: **new** Create Company Directory Contact
 - `UpdateDirectoryContact`: **new** Update Company Directory Contact
 - `DeleteDirectoryContact`: **new** Delete Company Directory Contact
 - `GetDirectoryContact`: **new** Get Company Directory Contact
 - `GetDirectoryContactsList`: **new** Get list of Company Directory Contact
 - `DeleteDirectory`: **new** Delete Company Directory (with all Company Directory contacts)

**InstantMessaging service:**

 - `AnswerToUrgentMessage`: **new** To answer to an urgent message ("Acknowledged" / "Ignored)


### [2.6.2] - LTS - 2022-01-27
---

In this version, a code reorganization has been done about WebRTC features:
 - some classes/event have been renamed or changed of namespace
 - some methods have moved

If don't use **WebRTC features**, you are not impacted.

If you use the **SDK.Wpf.WebControl** component, you are not impacted too since this component has been also updated to provide same features.

**You are only impacted if you directly use the WebRTC service.**
 
### Code reorganization and enhancements

Some classes are now in a different namespaces and/or have been renamed:

 - **Rainbow.WebRTC** is now **Rainbow.Jingle**
 - **Rainbow.Jingle.IceCandidate** is now **Rainbow.Model.Jingle.IceCandidate**
 - **Rainbow.Jingle.JingleSession** is now **Rainbow.Model.Jingle.JingleSession**
 - **Rainbow.Jingle.IceCandidatesEventArgs** is now **Rainbow.Events.IceCandidatesEventArgs**
 - **Rainbow.Jingle.RemoteDescriptionEventArgs** is now **Rainbow.Events.RemoteSDPEventArgs**

Some methods have been also moved, check the API update details below.

In this version, the way to manage WebRTC signaling messages (i.e. Jingle messages) has been improved. It's now possible to manage several WebRTC communication in same time. 

**SDK.Wpf.WebControl** has been updated to use this version even it don't offer the possibility to have several WebRTC communications. Same features are still available. 

In fact **SDK.Wpf.WebControl** will be soon obsolete when the current work in progress will be finished: it will offer the possibility to make WebRTC communication not only in **WPF** context but also in **Windows.Forms** and **UWP** context. 

It's based on modules 100% written in C# or modules available in multi-platform environments. So soon it will be also possible to have WebRTC communication in **Mac OS**.

Next steps will be to have the same for Android and iOS.

### Fixes:

 - **Bubbles**: Fix **ConferenceJoin()** method when used in WebRTC context.
 - **FileStorage**: Take into account recorded file (using **ConferenceRecordingStart()** in Bubbles service. A new event has been added: **RecordingFileUpdated**.
 - **Jingle to media parsing**: Fix about default port used and **ssrc** options
 - **SDP to Jingle**: Fix in **fmtp** parsing

### API update in details:

**Events**
 - `IceCandidatesEventArgs`: **update** Id property has been added to link it to a Call
 - `RemoteSDPEventArgs`: **update** Id property has been added to link it to a Call 

**Call class:**

 - `WebRtcCall`: **updated** To create a Call object in WebRTC context - Now it's not mandatory to specify an Id
 - `LocalMedias`: **renamed** Property has been renamed (before it was **LocalMedia**)
 - `RemoteMedias`: **renamed** Property has been renamed (before it was **RemoteMedia**)
 - `IsInProgress`: **new** To know if the call is in progress (i.e. not in status UNKNOWN or ERROR)
 - `IsRinging`: **new** To know if the call is ringing (i.e. in status RINGING_INCOMING or RINGING_OUTGOING)
 - `IsQueued`: **new** To know if the call is queued (i.e. in status QUEUED_INCOMING or QUEUED_OUTGOING)
 - `IsConnected`: **new** To know if the call is connected (i.e. IsInProgress() && (!IsConnecting()) && (!IsRinging()) )
 - `IsConnecting`: **new** To know if the call is connecting (i.e. in status DIALING or CONNECTING)
 
**Jingle service:** (previously it was **WebRTC service**)

 - `AddMedia`: **removed** Use instead **Util.AddMedia**
 - `RemoveMedia`: **removed** Use instead **Util.RemoveMedia**
 - `IsAudioUsed`: **removed** Use instead **Util.MediasWithAudio**
 - `IsAudioOnlyUsed`: **removed** Use instead **Util.MediasWithAudioOnly**
 - `IsAudioVideoOnlyUsed`: **removed** Use instead **Util.MediasWithAudioAndVideoOnly**
 - `IsVideoUsed`: **removed** Use instead **Util.MediasWithVideo**
 - `IsSharingUsed`: **removed** Use instead **Util.MediasWithSharing**
 - `IsSharingOnlyUsed`: **removed** Use instead **Util.MediasWithSharingOnly**
 
 - `SetBusyPresenceAccordingMedia`: **removed** Use instead **Contacts.SetBusyPresenceAccordingMedias**
 - `RollbackPresence`: **removed** Use instead **Contacts.RollbackPresenceSavedFromCurrentContact**

 - `Jingle_RemoteSDPReceived`: **renamed** Before it was **Jingle_RemoteDescriptionReceived**
 - `Jingle_CreateAnswer`: **updated** Event: Id property has been added to link it to a Call
  
 - `MakeCall`: **updated** The current presence is no more automatically stored  (to rollback it when the call is finished) since multi webRTC calls are now supported.
 - `AcceptIncomingCall`: **updated** The current presence is no more automatically stored  (to rollback it when the call is finished) since multi webRTC calls are now supported.
 - `Retract`: **new** Send message to the remote that the call has been retracted

 - `CheckMediasForMakeCall`: **updated** Now more medias configuration are allowed to make a call: we can manage more cases than UCaaS clients
 - `AcceptIncomingCall`: **updated** Now more medias configuration are allowed to answer a call: we can manage more cases than UCaaS clients
 - `CheckMediasForAnswerCall`: **new** To check if medias specified can be used to answer to a call

 - `CanAddLocalAudioTocall`: **renamed** Before it was **AddLocalAudioTocall**
 - `CanAddLocalVideoTocall`: **renamed** Before it was **AddLocalVideoTocall**
 - `CanRemoveLocalVideoFromcall`: **renamed** Before it was **RemoveLocalVideoFromcall**
 - `CanAddLocalSharingTocall`: **renamed** Before it was **AddLocalSharingTocall**
 - `CanRemoveLocalSharingFromcall`: **renamed** Before it was **RemoveLocalSharingFromcall**

**Contacts service:**

 - `PresenceInfoAreEquals`: **new** Check if Presence Info provided are equals
 - `SetPresenceLevel`: **updated** Now does nothing if Presence Level specified is the same the one already set.
 - `SetBusyPresenceAccordingMedias`: **new** Set presence of the current user according the list of medias provided.
 - `SavePresenceFromCurrentContactForRollback`: **new** Save presence of the current contact for rollback purpose - Can be restored using **RollbackPresenceSavedFromCurrentContact** 
 - `RollbackPresenceSavedFromCurrentContact`: **new**  Rollback presence previously set by **SavePresenceFromCurrentContactForRollback**
 - `GetPresenceSavedFromCurrentContactForRollback`: **new** Get presence previously set by **SavePresenceFromCurrentContactForRollback**

**FileStorage service:**

 - `RecordingFileUpdated`: **new** Event fired when a recording is performed and when files associated are avaialble / in progress.
 
**LogFactory service:** 

 - `Get`: **new** To get the current ILogger.

**Util calls:**

 - `AddMedia`: **new** To add a media in the list of medias provided
 - `RemoveMedia`: **new** To remove a media in the list of medias provided
 - `MediasWithAudio`: **new** To check if Audio is used in medias specified.
 - `MediasWithAudioOnly`: **new** To check if ONLY Audio is used in medias specified.
 - `MediasWithAudioAndVideoOnly`: **new** To check if ONLY Audio AND Video are used in medias specified.
 - `MediasWithAudioAndSharingOnly`: **new** To check if ONLY Audio AND Sharing are used in medias specified.
 - `MediasWithVideo`: **new** To check if Video is used in medias specified.
 - `MediasWithVideoOnly`: **new** To check if ONLY Video is used in medias specified.
 - `MediasWithSharing`: **new** To check if Sharing is used in medias specified.
 - `MediasWithSharingOnly`: **new** To check if ONLY Sharing is used in medias specified.
 - `GetSDKFolderPath`: **new** Return the folder path where the SDK is stored / used.
 

### [2.6.1] - LTS - 2021-12-08
---
### API update in details:

**Bubbles service:**
 - `IsArchived`: **update** return now a **Boolean?**, now it's also possible to use a **Bubble object** or a **Bubble Jid**
 - `IsMember`: **update** return now a **Boolean?**, now it's also possible to use a **Bubble object** or a **Bubble Jid**

**Bubble class:**
 - `MemberStatus`: **update** Add **Deleted** status
 
 
### [2.6.0] - LTS - 2021-11-18
---
### Enhancements / Fixes:

**Use logging abstraction**

In previous release, **Nlog** was used internally in the SDK for log purpose.

In this release, the SDK now use the logging abstraction introduced by Microsoft called **MEL** (**Microsoft.Extensions.Logging**) . It's available in **.Net Core** so it's **multi-platform**. 

So now you can use any back-end log providers based on **MEL** (non-exhaustive list):
- [Sentry](https://github.com/getsentry/sentry-dotnet) provider for the Sentry service
- [Serilog](https://github.com/serilog/serilog-framework-logging) provider for the Serilog service
- [elmah.io](https://github.com/elmahio/Elmah.Io.Extensions.Logging) provider for the elmah.io service
- [Loggr](https://github.com/imobile3/Loggr.Extensions.Logging) provider for the Loggr service
- [NLog](https://github.com/NLog/NLog.Extensions.Logging) provider for the NLog service
- [Graylog](https://github.com/mattwcole/gelf-extensions-logging) provider for the Graylog service
- [Sharpbrake](https://github.com/airbrake/sharpbrake#microsoftextensionslogging-integration) provider for the Sharpbrake service
- [KissLog.net](https://github.com/catalingavan/KissLog-net) provider for the KissLog.net service

The documentation has been updated and provide two examples to use **Nlog** and **SeriLog** as back-end.

**Management of Emergency Message has changed**

In a one to one conversation, an emergency message can be sent only if the peer has the **Rainbow Alert license**.

In a bubble, an emergency message can be sent only if one its member has the **Rainbow Alert license**.

The SDK provides two methods to know if it's allowed (in **InstantMessaging** service):
- Boolean **CanSendMessageWithHighUrgencyToConversation**(String conversationId)
- Boolean **CanSendMessageWithHighUrgencyToConversation**(Conversation conversation)

In consequence two capabilities has been removed (in **Contact.Capability**)
- **MessageUrgencySending**
- **MessageUrgencyReception**   

It's possible to know if a contact has a Rainbow Alert license using the property **IsAlertNotificationEnabled** of **Contact** object.

It's possible to know if a buble has at least one member with a Rainbow Alert license using the property **IsAlertNotificationEnabled** of the **Bubble** object.

**New API / Objects about Conferences**

You can now lock, unlock, delegate a Conference. Have talking time info and manage recording (start, stop, pause, resume) 

**All examples updated**

All examples have been updated to use this new LTS version and this new logging abstration.   

### API update in details:

**Bubbles service:**
- `ConferenceRemoved`: **new** Event fired when a conference is removed
- `ConferenceLockOrUnlocked`: **new** Lock or Unlock the conference - If locked, it disables any future participant from joining conference
- `ConferenceIntoWebinar`: **new** Changes conference into webinar
- `ConferenceDelegate`: **new** Current owner of the conference delegates its control to another participant (a moderator)
- `ConferenceTalkingTime`: **new** Get talkingtime of each participant in the conference. All webrtc participants talkingtime value (and more) will be returned even if the conference is ended.
- `ConferenceSubscribeToParticipantVideoStream`: **new** Gives the possibility to a user participating in a webrtc conference identified by 'conferenceId' to subscribe and receive a video stream published by a participant identified by 'participantId'.
- `ConferenceRecordingStart`: **new** The Start recording command initiates a recording of a conference for the user whom sent the request.
- `ConferenceRecordingStop`: **new** The Stop recording command stops a recording of a conference for the user whom sent the request.
- `ConferenceRecordingPause`: **new** The Pause recording command pauses a recording of a conference for the user whom sent the request.
- `ConferenceRecordingResume`: **new** The Resume recording command resumes a recording of a conference for the user whom sent the request.

**InstantMessaging service:**
- `CanSendMessageWithHighUrgencyToConversation`: **new** (by id) To know if it's possible to send a message in a conversation in High Urgency mode.(i.e. Emergency message)
- `CanSendMessageWithHighUrgencyToConversation`: **new** (by conversation) 

**Bubble object:**
 - `IsAlertNotificationEnabled`: **new** When set to true, allows participants in the room to send message with UrgencyType = UrgencyType.High (i.e. Emergency message)
 
 **Contact object:**
 - `IsAlertNotificationEnabled`: **new** Is user subscribed to "Rainbow Alert Offer". Only returned if retrieved user data corresponds to logged in user or if logged in user is in the same company than the retrieved user.

### [2.5.2] - STS - 2021-10-20
---

### Enhancements / Fixes:

**AutoReconnection service:**

- This service can now use previous credentials for auto reconnection purpose if the user token is no more valid. (optional using **UsePreviousLoginPwd** property - **True** by default).
- This service can now be used also to **start login process** using previous credentials (using user token first and then login/pwd if **UsePreviousLoginPwd** property is set).  
- The reconnection is automatically performed if the user credentials or user token has been accepted at least once by the server. In previous release, it was necessary to have at least a full initialization process performed. But this step could failed if the server is in trouble or if the network connection used is not very stable.
- This service try always to resume XMPP stream (if set/enabled) in reconnection or login process. 

**SdkError object**

- **ResponseStatus** property is now set to **ResponseStatus.None** by default (before is was **ResponseStatus.Completed**)
- **HttpStatusCode** property is now set to **0** by default (before is was **HttpStatusCode.OK** i.e. **200**)
- These two updates make it possible to deal with errors more effectively: due to network problems, errors returned by the server or errors reported by the SDK itself (exceptions or incorrect parameters)

**SdkIncorrectUseError object**

- **ErrorDetailsData** property has been added because details can be set by the server to better understand the error.

**Server Errors list**

- A full list of possible errors returnd by the server is now available in the documentation.


### API update in details:

***AutoReconnection service***:

- `IsStarted`: **new** (Property) To know if this service has started or not
- `UsePreviousLoginPwd`: **new** (Property) To use previous credentials if the user token can no more be used by the service for auto reconnection purpose  
- `IsOneNetWorkUp`: **new** Check Network interfaces to know if at least one network interface is up.
- `LoginUsingPreviousCredentials`: **new** Try to connect to the server using the previous credentials. If this is not possible due to a network problem, the service will make several attempts.

**"Application" service:**

- `DataCleared`: **new** Event raised when data in cache are cleared
- `AuthenticationSucceeded`: **new** Event raised when authentication with the server has succeeded (using a token or login/pwd)

**SdkIncorrectUseError object**
 
- `ErrorDetailsData`: **new** (Property) Error details data


### [2.5.1] - STS - 2021-09-29
---

### Enhancements / Fixes:

**Single Sign On (SSO):**

- Add methods to permit SSO (SAML and Open ID Connect are both supported). The multiplatform example use them to allow SSO in Windows, Android or iOS environment

**"AutoReconnection" service:**

- Once first connection and first initalization are performed, this new service (if enabled) will automatically try to reconnect to the server when the connection is lost.
- Network interfaces are checked to ensure attempts are performed only when at least one is operational and connected to a network.
- It's possible to set the maximum number of attempts before this service stops and even define a specific delay between each attempt.
- Each attempt uses the user token. This token is also automatically renew at its half-life by this service until it's no more possible (event **TokenExpired** is raised)

**Stream Management (XEP-0198): (Alpha version)**

- This new service (if enabled and if even pipe mode is set to XMPP) permits to reconnect to the XMPP server faster (using "resume" feature)
- Once connected again , the client receives only stanza (i.e. messages, presences, ...) not received between the disconnection and the reconnection.
- So all data cache internally used in the SDK is not cleared when this mode is enabled and the resume succeeded
- It's still a work in progress so it must not be used yet in production.
- At term, the support of this XEP will allow to have Push Notification in Android and iOS environment.

**Fixes:**

- Login was not stored in cache when using connection with token
- Server could not answer a valid JSON string (in maintenance mode for example) so cathc exception when trying to parse result error.

**Deprecated events / methods removed:**

- ***Events removed***: ContactAvatarChanged, ContactAvatarDeleted, ContactInfoChanged, ContactAdded, RosterContactAdded, RosterContactRemoved
- ***Use instead events***: PeerAvatarChanged, PeerAvatarDeleted, PeerInfoChanged, PeerAdded, RosterPeerAdded, RosterPeerRemoved
- ***Method removed***: GetAllContacts
- ***Use instead method***: GetAllContactsFromCache

### API update in details:

***Application root service***:

- `GetAuthenticationSSOUrls`: **new** To get list of URLs for SSO purpose
- `LoginWithToken`: **update** Add a parameter to avoid auto-logon to the event event pipe. So it can be done manually later.  
- `RenewToken`: **new** To renew the current user token

***Administration service***:

- `GetApplications`: **new** Get list of Rainbow Applications info
- `GetApplication`: **new** Get Rainbow Application info
- `UpdateApplication`: **new** Update Rainbow Application info  

***Telephony service***:

- `GetVoiceMessagesNumber`: **new** Get voice messages counters: total and unlistened.
- `ACDLogon`: **new** This api allows an ACD Agent to logon into the CCD system.
- `ACDLogoff`: **new** This api allows an ACD Agent to logoff from the CCD system.
- `ACDStatus`: **new** This api allows an ACD Agent to get its device's current ACD status.
- `ACDWithdrawal`: **new** This api allows an ACD Agent to change to the state 'Not Ready' on the CCD system.
- `ACDWrapUp`: **new** This api allows an ACD Agent to change to the state Working After Call in the CCD system.


### [2.5.0] - LTS - 2021-09-08
---

### Fix / Enhancement:

- Enhancement: 
    - New Capabilities: 
        - ReadReceipt: To know if Read Receipts must be visible to the user
        - UseGif: To know if Gif can be sent by the user
    - Previous File capability is now split in two:
        - FileStorage: To know if the user can use the Rainbow File Storage / Files service
        - FileSharing: To know if the user, in a conversation, can Send (upload), Save, Forward or Share file (available only if FileStorage is available)
- Fix: File download is always possible even if capabilities FileStorage and FileSharing are both set to false
- Fix: Edited message store now the date of the original message       

### API update in details:

***FileStorage service***:

- `CopyFileToPersonalStorage`: **new** Copy the file using its file descriptor id to the personal storage of the current user


### [2.4.0] - LTS - 2021-08-12
---

### Focus on Mutli-platform UI component

Still a work in progress but now an accurate milestone can be provided to have first UI components available for Android, iOS, Windows and MacOs: October / November.

For example the "MessageStream" component will allow to send / receive messages for the specified conversation. The layout and features available will be is nearly the same than the official  Web / Desktop client (about IM purpose) like you can see in this 3 screenshots:

[Web / Desktop client](https://github.com/Rainbow-CPaaS/Rainbow-CSharp-SDK-Samples/tree/master/MultiPlatformApplication/images/MessageStream-WebClient.png)

[SDK C# - On Windows (UWP)](https://github.com/Rainbow-CPaaS/Rainbow-CSharp-SDK-Samples/tree/master/MultiPlatformApplication/images/MessageStream-CSharp-UWP.png)

[SDK C# - On Android](https://github.com/Rainbow-CPaaS/Rainbow-CSharp-SDK-Samples/tree/master/MultiPlatformApplication/images/MessageStream-CSharp-Android.png)

More info about this actual work in progress [here](https://github.com/Rainbow-CPaaS/Rainbow-CSharp-SDK-Samples/tree/master/MultiPlatformApplication). 

### Fix / Enhancement:

- **XMPP XEP-0153** (vCard-Based Avatars): standard presence message was not taken into account when receiving node with this namespace "vcard-temp:x:update". It could lead to have an incoreect Contact Presence or/and incorrect Contact Aggregated Presence      
- Fix: Connection / disconnection troubles in some race conditions
- Fix: When option "mark automatically message as read" is set, avoid to mark message as read when they are sent by the current user using another device

### Dependencies

Bump **Newtonsoft.Json** package to **13.0.1** from **12.0.3**

Bump **NLog** to **4.7.10** from **4.7.7**

Bump **RestSharp** to **106.12.0** from **106.11.7**

Bump **System.IdentityModel.Tokens.Jwt** to **6.12.0** from **6.8.0**

### API update in details:

***Contacts service***: 

- `GetPresencesFromContact`:
- `GetPresencesFromContactId`:
- `GetPresencesFromContactJid`:
- `GetPresencesFromCurrentContact`:
- `GetAggregatedPresenceFromContact`:
- `GetAggregatedPresenceFromContactId`:
- `GetAggregatedPresenceFromContactJid`:
- `GetAggregatedPresenceFromCurrentContact`: **update** These 8 methods now return a copy of the internal object
- `GetPeerFromContactId`: **new** Get Peer object using contact Id

- `PeerAdded`: **new** Event raised when a Peer is added
- `PeerInfoChanged`: **new** Event raised when a Peer information has been updated on server side (and not when its presence level changed).
- `PeerAvatarChanged`: **new** Event raised when an avatar's peer has been updated on server side.
- `PeerAvatarDeleted`: **new** Event raised when an avatar's peer has been deleted on server side.
- `RosterPeerAdded`: **new** Event raised when a Peer has been added in your roster. The associated Contact is automatically added in the cache.
- `RosterPeerRemoved`: **new** Event raised when a Peer has been removed from your roster. The associated contact is not removed from the cache.

- `ContactAdded`: **deprecated** Use instead PeerAdded
- `ContactInfoChanged`: **deprecated** Use instead PeerInfoChanged
- `ContactAvatarChanged`: **deprecated** Use instead PeerAvatarChanged
- `ContactAvatarDeleted`: **deprecated** Use instead PeerAvatarDeleted
- `RosterContactAdded`: **deprecated** Use instead RosterPeerAdded
- `RosterContactRemoved`: **deprecated** Use instead RosterPeerRemoved

***FileStorage service***:

- `CreateFileDescriptor`: **new** To create a FileDescriptor using a file path or a file stream. (it was a private method before. It's used internally by `UploadFile`)
- `UploadFile`: **new** To upload a file using a Stream object with or without using a FileDescriptor. It's still possible to upload a file with a valid file path.

***InstantMessaging service***:

- `SendMessage`: **new** (Low level API) To send an IM with a Conversation object and a Message object. It's easier to use one of 15 others method (at least) to send IM. 
- `SendMessageWithFileToConversationId`: **new** To send an IM with a file using a Stream object. It's still possible to send an IM with a valid file path.

***Message object***:

- `FromText`: **new** To create message object with text as content (it was a private method before)
- `FromTextAndFileDescriptor`: **new** To create message object with text and a file as content (it was a private method before)


### [2.2.0] - LTS - 2021-06-09
---

### Focus on samples

Two new samples have been added:

- **Mass Provisioning**: [sample here](https://github.com/Rainbow-CPaaS/Rainbow-CSharp-SDK-Samples/tree/master/MassProvisioning)

This sample is based on this scenario: An organization admin needs to manage several University with a lot of Students and some Teachers. Each Teacher has several Classrooms (i.e Bubbles) with a certain numbre of Students

For each University a Company is created with a Company Admin.

This Company Admin is used to create all teachers and  all Students of the University.  

Then using each Teacher, Classrooms are created and Students are assigned to each Classroom.

It takes less than 11 minutes to create a Univertisty, (i.e. a Company) with 1 Company Admin, 912 Students, 33 Teachers with 6 Classrooms by teacher and 25 students in each classroom.

This sample also demonstrates how to use asynchronous methods of the SDK API in a synchronous way.

- **Multiplatform application**: [sample here](https://github.com/Rainbow-CPaaS/Rainbow-CSharp-SDK-Samples/tree/master/MultiPlatformApplication)

This sample demonstrates how to use the C# SDK to create iOS, Android and UWP (Windows) applications sharing more than 98% on common code.

It is based on MVVM architecture and we plan add step by step all features available in current iOS / Android application. So stay tuned :)

[Screenshot here](https://github.com/Rainbow-CPaaS/Rainbow-CSharp-SDK-Samples/blob/master/images/MultiplatformApplication.png?raw=true)

### Fix:

- Aggregated presence not updated when telephony presence is changed. Events ContactPresenceChanged and ContactAggregatedPresenceChanged are then raised correctly.

***Application root service***: **updated**

- `GetUserSubscriptions`: **new** To know the list of subscriptions of the current user

### [2.1.0] - LTS - 2021-05-24
---

### Login process enhanced:

Less REST API are called in the login process.

Allow to log on the server in 2 steps: (To use only for advanced cases)
- First for authentication purpose (with `LoginWithoutEventPipe` or `LoginWithTokenWithoutEventPipe`)
- Then to connect to the event pipe (with `LoginToEventPipe`)
Between these 2 steps, it's possible to use any methods using RESt API but until event pipe connection is performed no events will be received from the server (IM message, presence, etc ...)


***Application root service***: **updated**

- `GetApplicationTokenFromCache`: **new** To get all info about the ApplicationToken object used as authentication purpose with the Rainbow Server.
- `LoginWithoutEventPipe`: **new** Allow 
- `LoginWithTokenWithoutEventPipe`: **new** Allow
- `LoginToEventPipe`: **new** Allow

***ApplicationToken object***: **new**

Describe the application token used as authentication purpose with the Rainbow Server.

***Administration service***: **updated**

- `CreateUser`: **fix** Parameter 'isCompanyAdmin' was not correctly managed.

### [2.0.0] - LTS Beta - 2021-04-23
---

### Dependency updated:
Remove WebSocketSharp-netstandard dependency and use instead System.Net.WebSockets.CLientWebSocket. 
 
In consequence **`Windows 7 and Windows 8`** are no more supported

***Instant Messaging service***: **updated**
- `ClearAllMessagesFromBubble[Id,Jid]FromCache`: **new** Clear all messages already stored in cache from the conversation with this bubble
- `ClearAllMessagesFromBot[Id,Jid]FromCache`: **new** Clear all messages already stored in cache from the conversation with this bot
- `ClearAllMessagesFromContact[Id,Jid]FromCache`: **new** Clear all messages already stored in cache from the conversation with this contact
- `ClearAllMessagesFromConversation[Id]FromCache`: **new** Clear all messages already stored in cache from this conversation


### [1.22.2.0] - 2021-04-01
---

### Capabilities: **added**
New capability has been added. Related services have been updated to take it into account.
 - `RecordingConversation` **new** To know if the user can record conversation in WebRTC

### Event Mode: **added**
A new event mode has been added to the two already existing:
- `SDKEventMode.XMPP`: to use XMPP protocol for eventings purpose (default).  
- `SDKEventMode.S2S`: to use "server to server" (S2S) architexture for eventings purpose.

The new one `SDKEventMode.NONE` permits to used the SDK without any eventings. Very usefull if you use this SDK to perform only REST requests.

***WebRTC service***: **updated**
Base class to manage WebRTC Signaling
- `MakeCall` **updated** Allow to set a display name and a subject (optional) when making a call
- `RollbackPresence` **new** Method now public - Rollback presence previously set by **SetBusyPresenceAccordingMedia()**


***Alerts service***: **fixed**
- `AlertMessageReceived` **updated** Event raised even if a previous alert with same identifier is received but only if the date is more recent


### [1.22.1.0] - 2021-03-11
---

### New features
A new component **[Rainbow.CSharp.SDK.Common](https://www.nuget.org/packages/Rainbow.CSharp.SDK.Common/)** has been created.

It's a mulitplatform component (Windows, MacOS, iOS, Android, Linux, ...) which provide new optional services.

The first release permits to easily manage Avatars of Contacts or Bubbles (using Rainbow.Common.Avatars service). It downloads them automatically (avatar deleted, updated by end-users) and stored them locally for reuse purpose. 
If a bubble has not a specific avatar, this service create automatically it using Avatars members.

This package includes also an ImageTools library which can re-used for other needs.

The [WebRtcControl Sample](https://github.com/Rainbow-CPaaS/Rainbow-CSharp-SDK-Samples/tree/master/Windows_WPF/WPF_WebRtcControl) used to demonstrate WebRtc in Peer To Peer context has been updated to use this new package to display avatar of the peer. 

### Dependencies:
- `System.IdentityModel.Tokens.Jwt` v6.8 is now used (instead of v6.7.1)
 
### Capabilities: updated / added
Some previous capabilities have been updated. New capabilities have been added. Related services have been updated to take them into account.
 
- `Bubble` **replace** `BubbleCreate` and `BubbleBubbleParticipate` - To know if the user can use Bubbles
- `File` **replace**  `FileUpload` and `FileDownload` - To know if the user can send/receive messages with Files
- `InstantMessaging` **new** - To know if the user can use Instant messaging 
- `Channel` **new** - To know if the user can use Channels
- `WebRTCLocalSharing` **new** - To know if the user can use local sharing in WebRTC
- `WebRTCAudio` **new** - To know if the user can use audio in WebRTC
- `WebRTCVideo` **new** - To know if the user can use video sharing in WebRTC
- `SoftPhoneOnly` **new**  - To know if the user can use only soft phone features (no IM, bubble, channel, etc ...
- `MessageUrgencySending` **new** - To know if the user can send IM message typed as UrgencyType.High
- `MessageUrgencyReception` **new** - To know if the user can receive IM message typed as UrgencyType.High 

## [1.22.0.0] - 2021-02-18
---

### Dependencies:
- `NLog` v4.7.7 is now used (instead of v4.7.2)
- `RestSharp` v106.11.7 is now used (instead of v106.11.4)

### Robustness:
- Login process fail if the SDK cannot get Company info of the current user

### New features
Several objects/services have been added to manage WebRTC signaling process (SDP, Ice Candidate, etc ...) using Jingle Message. Medias ARE NOT managed by the SDK.

It necessary to use another package to offer full WebRtc features (so adding MEdia and using this SDK for the signaling process) 

A first release of this kind of package is available: [Rainbow.CSharp.SDK.Wpf.WebRtcControl](https://www.nuget.org/packages/Rainbow.CSharp.SDK.Wpf.WebRtcControl/). It allows WebRtc (Audio, Video, Sharing) in Peer To Peer for WPF application

A full sample using this control can be found here: [WebRtcControl Sample] (https://github.com/Rainbow-CPaaS/Rainbow-CSharp-SDK-Samples/tree/master/Windows_WPF/WPF_WebRtcControl)

### Roadmap
A focus will be done to provide reuseable UI controls to create easily application using these them:
- Avatar : to display easily Contact avatar with presence /counters, to display easily Bubble avatar 
- RecentConversations: To display a resume of last conversation ( avatars, last message, ...) 
- ConversationStream: To display the conversation, to read / send message, share/get files ...
- Systray: Systray integration
- NotificationWindow
- ...

To do this the CSharp SDK will be splitted with several packages:
- SDK (Core): the current one - multi-platform
- SDK WPF: UI Controls (Like WebRtcControl) for for windows (using WPF) 
- SDK MacOS: UI Controls for MacOs (using Xamarin)
- SDK Android: UI Controls for Android (using Xamarin)
- SDK iOS: UI Controls for iOS using Xamarin)

A full sample (for each platfrom) with all UI controls will be available to demonstrate their usage.

Each time a full setup will be available (and the way to create it)

First step will target is Windows using WPF. Then MacOS.

Before to offer same things for Android / iOS, XMPP Stream management will be added in the SDK (core) first since it permits to have a better way to handle disconnection problems

### Update(s)) / fixe(s) in this release 

***WebRTC service***: **new**
Base class to manage WebRTC Signaling
- `AcceptedCall` **new** Event -  Call accepted by remote
- `AcceptedCallOnAnotherResource` **new** Event - Call accepted on another resource  
- `ErrorRaised` **new** Event - Error Raised 
- `IncomingCall` **new** Event - Incoming call 
- `Jingle_ContentChanged` **new** Event - Jingle Content Changed 
- `Jingle_CreateAnswer` **new** Event - Jingle Create Answer
- `Jingle_IceCandidatesReceived` **new** Event - Jingle Ice Candidates Received
- `Jingle_MediaChanged` **new** Event - Jingle Media Changed 
- `Jingle_RemoteDescriptionReceived` **new** Event - Jingle Remote Description Received
- `RejectedCall` **new** Event - Call Rejected
- `ReleasedCall` **new** Event - Call Released 
- `RetractedCall` **new** Event - Call Retracted
- `AcceptIncomingCall` **new** Method - Accept Incoming Call
- `AddLocalAudioTocall` **new** Method - Add Local Audio To Call 
- `AddLocalSharingTocall` **new** Method - Add Local Sharing To call
- `AddLocalVideoTocall` **new** Method - Add Local Video To call
- `AddMedia` **new** Method - Add media to medias list  provided
- `CheckMediasForMakeCall` **new** Method - Check if medias list is correct to make a call 
- `GetJingleSessionFromCache` **new** Method - Get Jingle session from cache 
- `GetTurnServersConfiguration` **new** Method - Get Tunrs Server configuration 
- `HangUp` **new** Method - Hang up
- `IsAudioOnlyUsed` **new** Method - Check if only Audio is used in medias list provided
- `IsAudioUsed` **new** Method - Check if Audio is used in medias list provided
- `IsAudioVideoOnlyUsed` **new** Method - Check if only Video is used in medias list provided
- `IsSharingOnlyUsed` **new** Method - Check if only Sharing is used in medias list provided
- `IsSharingUsed` **new** Method - Check if Sharing is used in medias list provided
- `IsVideoUsed` **new** Method - Check if Video is used in medias list provided
- `MakeCall` **new** Method - Make call
- `MuteLocalAudio` **new** Method - Mute local audio
- `MuteLocalSharing` **new** Method - Mute local sharing
- `MuteLocalVideo` **new** Method - Mute local video
- `OneMediaSpecifiedOnly` **new** Method - Check if only one media is used in medias list provided
- `RejectIncomingCall` **new** Method - Reject Incoming Call
- `RemoveLocalSharingFromcall` **new** Method - Remove Local Sharing From call
- `RemoveLocalVideoFromcall` **new** Method - Remove Local Video From call
- `RemoveMedia` **new** Method - Remove media from medias list provided
- `SendIceCandidate` **new** Method - Send Ice Candidate
- `SendJingleSessionMessage` **new** Method - SendJ ingle Session Message
- `SetBusyPresenceAccordingMedia` **new** Method - Set Busy Presence According Media

***Application service***: **update**
- `GetIniPathFolder` **new** To get folder path where INI file is stored

***Contacts service***: **update**
- `GetAllContactsFromCache` **new** Get all contacts from cache (will replace `GetAllContacts` )
- `GetAllContacts` **update** DEPRECATED: use an unnecessary callback

***Alert object***: **update**
- `DomainUsername` **new** To set Domain and User name of the device

## [1.21.1.0] - 2021-01-29
---

***Telephony service***: **fix**

The service was not started if the loggued user had a JidIm and a JidTel with a different domain name

***Contacts service***: **fix**

Presences aggregation was not correct for users with a JidIm and a JidTel with a different domain name 

## [1.21.0.0] - 2021-01-12
---

## API breaking changes
All methods to send messages (by contact, by bot, by bubble, by conversation with or wihtout files so in total 20 methods) has been slightly changed with a new parameter allowing to set message urgency.

By defaul, the urgency is set to `UrgencyType.Std`. There is 3 others level: `UrgencyType.Low` (for information message), `UrgencyType.Middle` (for important message) and `UrgencyType.High` (for critical message). 

***InstantMessaging service***: **update**
- `ForwardMessage` **new** To forward a message from one conversation to another
- `SendAlternativeContentsToConversationId` **update**
- `SendMessageWithFileToConversation` **update**
- `SendMessageWithFileToConversationId` **update**
- `SendMessageWithFileToContactId` **update**
- `SendMessageWithFileToContactJid` **update**
- `SendMessageWithFileToContact` **update**
- `SendMessageWithFileToBubbleId` **update**
- `SendMessageWithFileToBubbleJid` **update**
- `SendMessageWithFileToBubble` **update**
- `SendMessageToConversation` **update**
- `SendMessageToConversationId` **update**
- `SendMessageToBotId` **update**
- `SendMessageToBotJid` **update**
- `SendMessageToBot` **update**
- `SendMessageToContactId` **update**
- `SendMessageToContactJid` **update**
- `SendMessageToContact` **update**
- `SendMessageToBubbleId` **update**
- `SendMessageToBubbleJid` **update**
- `SendMessageToBubble` **update**

***Message object***: **update** 
- `UrgencyType` **new** To know message urgency: std (default), low, middle or high
- `IsForwarded` **new** To know is this message has been forwarded

***LogConfigurator service***: **update** 
- `GetLogger` **update** New parameter (optional) to allow to choose logger name. So it's now possible to configure several logger 

## [1.20.0.0] - 2020-12-15
---

***Channels service***: **fix**
- Fix bad parsing of events related to Channel

***Application service***: **update** 
- `SetResourceId` **new** Allow to specify the Resource Id used by the SDK to connect to the server 

***Restriction object***: **update** 
- `UseSameResourceId` **new** True to use the same "resource id" in XMPP messages each time the SDK is used. The INI File is used to store it. False by default 
- `MessageMaxLength` **update** Default value is now 16384 (from 1024 before)

***Contacts service***: **update** 
- `GetPresenceFromCurrentContact` **new** To know the presence of the current user using this SDK. GetPresencesFromCurrentContact() is still available to get the presence from all resources and GetAggregatedPresenceFromCurrentContact() to get a summary.  

## [1.19.0.0] - 2020-11-23
---

***Administration Service***: **update**
- `SendMessage` **new** To send a message on user behalf

***Alerts Service***: **update**
- `AlertMessageReceived` **fix** Avoid to raise event `AlertMessageReceived` for alerts already received from server with the same `Identifier`
- `SendAlertFeedback` **fix** Bad URI used
- `GetReportComplete` **new** To get the complete report on an Alert

***Channel object***: **update**
- `Mute` **new** To know if a channel is muted or not

***Channels service***: **update**
- `MuteOrUnmute` **new** To mute / Unmute a channel

***Bot object***: **new** 

Describe a Bot
- `Id` - Bot Id
- `Jid` - Bot Jid
- `Name` - Bot Name
- `AvatarId` - Bot Avatar Id
- `Capabilities` - List of capabilities tags
- `LastAvatarUpdateDate` - Date of last bot avatar create/update
- `CreatedByUserId` - User Id of the bot's creator
- `CreatedByLoginEmail` - Login email of the bot's creator
- `IsRainbowSupportBot` - True if this bot is the Rainbow support bot

***TV object***: **new** 

Describe a TV Device
- `Id` - Tv Id
- `UserId` - The User Id associated to this TV
- `Name` - Tv Name
- `Location` - Location of the TV
- `LocationDetail` - More details about the location of the TV.
- `Room` - Name of the room where the TV is located.
- `CompanyId` - Company Id linked to the TV.
- `ActivationCode` - Activation code (6 digits).
- `ActivationCodeGenerationStatus` - Activation code (6 digits).
- `CodeUpdateDate` - Date of last activation code update.
- `Status` - TV status
- `SubscriptionId` - Subscription to use when activating TV.

***CalendarState object***: **new**

Define the calendar state of a contact
- `Status` - The calendar status
- `Busy` - To know is contact is busy or not
- `Subject` - The meeting subject
- `Since` - The meeting since date
- `Until` - The meeting until date

***CalendarOutOfOfficeState object***: **new**

Define the calendar out of office state of a contact
- `Enabled` -  To know if "out of Office" is enabled
- `Busy` - To know is contact is busy or not
- `Start` - The "out of office" start date
- `End` - The "out of office" end date
- `Message_text` - The "out of office" message as plain text
- `Message_html` - The "out of office" message as HTML (if any)

***Contacts Service***: **update**
- `IsContactId` **new** To know if the specifid Id is related to a Contact stored in the cache
- `IsKnownId` **new** To know if the specifid Id is related to a Contact, a TV or a Bot stored in the cache
- `GetCalendarStateFromCurrentContact` **new** To get calendar state of the current contact
- `GetCalendarStateFromContactId` **new** To get calendar state of the contact id
- `GetCalendarStatesFromContactsId` **new** To get calendar states of the specified list of contact id
- `GetCalendarOutOfOfficeStateFromCurrentContact` **new** To get calendar "Out of office" state of the current contact 
- `GetCalendarOutOfOfficeStateFromContactId` **new** To get calendar "Out of office" state of the specified contact id
- `EnableCalendar` **new** To enable (or disable) calendar sharing of the current contact
- `GetAllTvs` **new** To get the all bots from cache
- `GetAllTvsFromServer` **new** To get all bots from server
- `GetTvFromTvId` **new** To get Bot from cache using its id
- `GetTvFromUserId` **new** To get Bot from cache using its jid
- `GetTvIdFromUserId` **new** To get Bot from cache using its id
- `IsTvId` **new** To know if the specifid Id is related to a TV stored in the cache
- `GetAllBots` **new** To get the all bots from cache
- `GetAllBotsFromServer` **new** To get all bots from server
- `GetBotFromBotId` **new** To get Bot from cache using its id
- `GetBotFromBotJid` **new** To get Bot from cache using its jid
- `GetBotIdFromBotJid` **new** To get Bot from cache using its id
- `GetBotFromBotIdFromServer` **new** To bot from server using its id
- `IsBotId` **new** To know if the specifid Id is related to a bot stored in the cache

***Conversations Service***: **update**
- `GetConversationFromBot` **new** To get conversation from bot object
- `GetConversationFromBotId` **new** To get conversation from bot id
- `GetOrCreateConversationFromBotId` **new** To create or get conversation from bot id

***InstantMessaging Service***: **update**
- `GetMessagesFromConversationId` **fix** Fix when a conversation id was related to a bot conversation
- `GetMessagesFromBot` **new** To get messages from bot object from server
- `GetMessagesFromBotId` **new** To get messages from bot id from server
- `GetMessagesFromBotJid` **new** To get messages from bot jid from server
- `GetAllMessagesFromBotFromCache` **new** To get messages from bot object from cache
- `GetAllMessagesFromBotIdFromCache` **new** To get messages from bot id from cache
- `GetAllMessagesFromBotJidFromCache` **new** To get messages from bot jid from cache
- `SendMessageToBot` **new** To get messages from bot jid
- `SendMessageToBotId` **new** To get messages from bot jid
- `SendMessageToBotJid` **new** To get messages from bot jid

***FileStorage Service***: **update**
- `DownloadFileFromFileDescriptor` **new** To download a file using FileDescriptor object

## [1.18.0.0] - 2020-11-04
---

***Administration Service***: **update**
- `CreateOrganisation` **new** Create an Organisation
- `UpdateOrganisation` **new** Delete an Organisation
- `DeleteOrganisation` **new** Update an Organisation
- `AddCompanyToOrganisation` **new** Add a Company in an Organisation
- `RemoveCompanyFromOrganisation` **new** Remove a Company from an Organisation
- `GetPresencesFromUser` **new** Get presences of an user on behalf (presence of all resources)

***Contacts Service***: **update**
- `GetAggregatedPresence` **new** Get aggregate presence from a list of presences (by resource). Usefull after using `Administration.GetPresencesFromUser()` method for example 

## [1.17.0.0] - 2020-10-14
---

***Major Update - Bubble Affiliation***

***RQRAINB-4164:*** **SDK CSharp [QUALITY] Enhance Bubble join mechanism to avoid message flood sent to the backend**

The process for joining the bubble has been updated to ensure that there is less stress on the server when connecting / reconnecting to the server

The action of "Join the bubble" is done by packet of 10 requests. The next packet is only requested after a response to all previous requests.

If an error occurs, the next packet is requested after a delay of 15 then 30, 60 and max 120 seconds if an error is again received from the server.

The delay is reset to 0 if no error is received managing a packet.

***Bubbles Service***: **update**
- `BubbleAffiliationChanged` **new** Event raised whan the current user has joined / unjoined a bubble
- `GetBubbleByJidFromCache` **new** To get bubble object from cache using its jid
- `isMember` **new** To know if the current used is an member of the specified bubble

***Contacts Service***: **update**
- `SearchContactByEmails` **update** Allow to search up to 1000  (***CRRAINBAMI-37:*** **SearchContactByEmails returns only 100 first elements**)

***Telephony Service***: **update**
- `GetMediaPillarInfo` **new** Permit to get MEdia Pillar info for the current user: RainbowPhoneNumber, RemoteExtension, Prefix, Jid (***CRRAINBAMI-39:*** **Request to expost the 17 Digit Rainbow User ID in case of Rainbow WebRTC Gateway**)   

## [1.16.0.0] - 2020-09-23
---

***Online Documentation***: **update**
- `Alerts service` menu entry added
- `Group model object` menu entry added
- `Groups service` menu entry added
- `IniFileParser object` menu entry added
- `S2SEventPIPE service` menu entry added

***Channels service***: **update**
- `GetDetailedAppreciations` **fix** Now asks server all detailed appreciations by pack of 100. (before only first 100 were returned)

***InstantMessaging service***: **update**
- `SendAlternativeContentsToConversationId` **fix** Always set a namespace in XMPP messsage when sending alternative contents (permits to send correctly `Adaptative Card` using `form/json` as Mime Type)

***Message object***: **update**
- `ToString` **fix** Fix correct output when alternative content is used 

## [1.15.0.0] - 2020-09-01
---

***Major Update - log4net package no more used*** 

`NLog` is now used as logging module and replaces `log4net` package in this release.
 
It's not possible to use rolling file for log purpose in MacOS using `log4net` package and this package is no more maintained since November 2017.

It's why `NLog` 4.7.2 has been choosen to replace it.   

In consequence, the way to log information has changed. You need to change your code like this:
 - `using log4net;` must be replaced by 'using NLog;' 
 - `DebugFormat`, `InfoFormat`, `WarnFormat` and `ErrorFormat` must be replaced by `Debug`, `Info`, `Warn` and `Error`.
 - `ILog log` must be replaced by `Logger log`
 
The configuration file and the initialisation step has changed too. Full documentation available [here](/#/documentation/doc/sdk/csharp/guides/130_application_logging)  
 
***Major Update - WebSocket4Net package no more used***

`WebSocketSharp-netstandard` is now used as WebSocket multi-platform purpose and replaces `WebSocket4Net`.

On MacOS, `WebSocket4Net` doesn't well manage network disconnection. At minimum, a delay of one minute was necessary to know if the network was disconnected. 

This impact only the way to manage connection if you are using a proxy. `Application.SetWebProxy` and `Application.SetWebProxy` no more exist. The method `Application.SetWebProxyInfo` must be used instead.
 
***Major Update - dependencies up to date***

All packages references used have been updated to use the last version available:
- `Newtonsoft.Json` 12.0.3 (10.0.1 before)
- `RestSharp` 106.11.4 (106.6.10 before)
- `System.IdentityModel.Tokens.Jwt` 6.7.1 (5.4.0 before)

Target Frameworks list of the SDK has also changed:
- `.Net Framework 4.6.1` as target has been removed (no impact since `.Net Standard 2.0` is still targeted)
- `.Net Core 3.0` is now targeted instead of .Net Core 2.0

***Administration service***: **update**
- `DeleteJoinCompanyLink` **new** To delete a `Join Company Link`
- `GetJoinCompanyLinks` **new** To get `Join Company Links` already created
- `CreateJoinCompanyLink` **fix** To create a `Join Company Link`

***Alerts service***: **update**

**update** A delay is internally used before to send receipts for Alert messages (received and read)  
- `SendAlertFeedback` **update** Send feedback to an AlertMessage received. DeviceId must now be provided as parameter

***Telephony service***: **update**
- `ConsultationCall` **fix** To make a consulation call (pb fix in OXO context)
- `MakeCall` **fix** To make a PBX call (pb fix in OXO context)
- `MakeCallToMevo` **fix** To consult its MEVO (pb fix in OXO context)
- `MakeCallWithSubject` **fix** To make a PBX call with a subject (pb fix in OXO context)

***Util object***: **update**
- `SerialiseSdkError` **deleted** Replaced by `SerializeSdkError` 
- `SerializeSdkError` **new**
- `SerializeFromResponse` **new** To serialize in string a IResponse or IREsponse&lt;T&gt; object

***SdkError object***: **update**
- `ResponseStatus` **new** Property to now the exact response status of REST Request (None, Completed, Error, TimedOut, Aborted)
- `HttpStatusCode` **new** Property to now the exact HTTP status code of REST Request

## [1.13.0.0] - 2020-06-26
---

***Major Update***

**Alerts service**: A dedicated service has been created - available using Application.GetAlerts()

Previous methods about Alerts available in InstantMessaging are no more available: They have moved in the new **Alerts service**

***Alerts service***: **new**
- `AlertMessageReceived` **new** Event raised when an AlertMessage is received 
- `MarkAlertMessageAsRead` **new** Mark AlertMessage as Read
- `CreateAlert` **new** Create and Send Alert using specified AlertTemplate and AlertFilter 
- `DeleteAlert` **new** Delete an Alert
- `UpdateAlert` **new** Update an Alert
- `GetAlert` **new** Get an Alert (by Id)
- `GetAlerts` **new** Get Alerts list
- `CreateDevice` **new** Create AlertDevice
- `DeleteDevice` **new** Delete AlertDevice
- `UpdateDevice` **new** Update AlertDevice
- `GetDevice` **new** Get AlertDevice (by id)
- `GetDevices` **new** Get AlertDevices list (by companyId)
- `GetDevicesTags` **new** Get AlertDevices Tags list (by companyId)
- `CreateFilter` **new** Create AlertFilter
- `DeleteFilter` **new** Delete AlertFilter
- `UpdateFilter` **new** Update AlertFilter
- `GetFilter` **new** Get AlertFilter (by id) 
- `GetFilters` **new** Get AlertFilters list
- `CreateTemplate` **new** Create AlertTemplate
- `DeleteTemplate` **new** Delete AlertTemplate
- `UpdateTemplate` **new** Update AlertTemplate
- `GetTemplate` **new** Get AlertTemplate (by id)
- `GetTemplates` **new** Get AlertTemplates list (by companyId)
- `GetReportDetails` **new** Get detailled report from a created Alert
- `GetReportSummary` **new** Get summary report from a created Alert
- `SendAlertFeedback` **new** Send feedback to a AlertMessage received

***AlertDevice object***: **new**
Only a AlertDevice previously created can receive an AlertMessage

***AlertTemplate object***: **new**
Define the content of the Alert to create/send. The same AlertTemplate can be used to create several Alerts.

***AlertFilter object***: **new**
Allow to send Alerts only to some AlertDevices (it's optional when creating an Alert)

***Administration service***: **update**
- `CreateSelfRegisteredUser` **update** Allow to create user using CompanyInvitationId(like before) or CompanyLinkID(new). It's now alos possible to set direclty first, last and nick name and isInitialized

***Contacts service***: **update**
- `SearchContactByEmails` **new** To search contacts by emails list

***InstantMessaging service***: **update**
- `AlertMessageReceived` **deleted**
- `AlertReceiptReceived` **deleted**
- `MarkAlertMessageAsRead` **deleted**
- `SendAlertMessage` **deleted**
- `UpdateAlertMessage` **deleted**
- `CancelAlertMessage` **deleted**

## [1.12.0.0] - 2020-06-08
---

***Major Update***
- Add Alert Message management: need to set Application.Restriction.AlertMessage to true. A specific server environment and rights are necessary.

***InstantMessaging service***: **update**
- `AlertMessageReceived` **new** Event raised when an alert message is received
- `AlertReceiptReceived` **new** Event raised when an alert message receipt (Received / Read) is received
- `MarkAlertMessageAsRead` **new** Mark alert message as Read
- `SendAlertMessage` **new** Send alert message
- `UpdateAlertMessage` **new** Update alert message previously sent
- `CancelAlertMessage` **new** Cancel alert message previously sent

***AlertMessage object***: **new**
- Use CAP (Common Alert Procotol) structure

***AlertMessageInfo object***: **new**
- Use CAP (Common Alert Procotol) structure

***Restriction object***: **update** 
- `AlertMessage` **new** To handle alert message (false by default)

***Application service***: **update**
- `Application` **update** (Constructor) Allow to specify INI File name to use ("configuration.ini" by default)

***Administration service***: **update**
- `GetOrganisations` **new** Get Organisations list
- `GetCompanies` **new** Add new parameter 'organisationId' (can be null) 
- `CreateUser` **new** Add new parameter 'nickName' (can be null) and 'isInitialized'
- `UpdateCompanyCustomData` **new** Add / Update custom data for a company
- `DeleteCompanyCustomData` **new** Delete custom data for a company
- 'GetRestClient' **new** Permits to get a RestClient object (Cf. RestSharp Nuget Package) if you need to perform specific REST operations  
 
***Organisation object***: **new**
- `Id` 
- `Name`
- `CreationDate`
- `Visibility`
- `IsDevelopers`

***Company object***: **update**
- `CustomData` **new** Custom Data specifically set to a company  

## [1.11.0.0] - 2020-05-12
---

***Application service***: **update**
- `SetWebProxy` **new** Allow to set WebProxy object (SetIPEndPoint method is still available)
- `GetAvailableThemes` **new** Get themes available for current user 
- `GetUserTheme` **new**  Get theme selected for the current user 
- `GetAvatarTheme` **new** Get avatar theme 
- `SelectTheme` **new** Select a theme 
- `UnselectTheme` **new** Unslect a theme 

***Administration service***: **update**
- `GetAvailableThemes` **new** Get themes available for the company
- `CreateTheme` **new** Create a Theme
- `DeleteTheme` **new** Delete a Theme
- `UpdateTheme` **new** Update a Theme
- `GetAvatarTheme` **new** Get avatar of the specified theme 
- `DeleteAvatarTheme` **new** Delete avatar of the specified theme
- `UpdateAvatarTheme` **new** Update avatar of the specified theme

***Util service***: **update**
- `CreateMD5` **new** Get MD5 from specified string

New methods to allow encryption / decryption of String. (before they were private)
- `Encrypt` **new** Encrypt the specified string
- `Decrypt` **new** Decrypt the specified string
- `SetEncryptionKey` **new** Set the key used for encryption (a default one is already set)

These methods are used internally: 
- To store password in INI File cache (cf. `GetUserPasswordFromCache()` in Application service)
- To read / write any encrypted values in INI file cache (cf. `GetEncryptValue()` and in `WriteEncryptValue()` in IniFileParser service)   
So set the encryption key in init step of your application/bot and doesn't change it when the process is running. 

## [1.10.0.0] - 2020-04-15
---

***Major Update***
- All signatures methods to send messages have slightly changed (in InstantMessaging service). One parameter has been added to allow to specify geolocation. This parameter can be null.

***Minor Update***
- Capabilities of the current user (`BubbleCreate`, `BubbleParticipate`, `FileUpload`, `FileDownload`, `UserNameUpdate` and `PhoneMeeting`) now take into account Company capabilities by inheritance when necessary.

***InstantMessaging service***: **update**
- `SendMessageToBubble` **update** New parameter Geolocation (can be null) 
- `SendMessageToBubbleId` **update** New parameter Geolocation (can be null)
- `SendMessageToBubbleJid` **update** New parameter Geolocation (can be null)
- `SendMessageToContact` **update** New parameter Geolocation (can be null)
- `SendMessageToContactId` **update** New parameter Geolocation (can be null)
- `SendMessageToContactJid` **update** New parameter Geolocation (can be null)
- `SendMessageToConversation` **update** New parameter Geolocation (can be null)
- `SendMessageToConversationId` **update** New parameter Geolocation (can be null)
- `SendMessageWithFileToBubble` **update** New parameter Geolocation (can be null)
- `SendMessageWithFileToBubbleId` **update** New parameter Geolocation (can be null)
- `SendMessageWithFileToBubbleJid` **update** New parameter Geolocation (can be null)
- `SendMessageWithFileToContact` **update** New parameter Geolocation (can be null)
- `SendMessageWithFileToContactId` **update** New parameter Geolocation (can be null)
- `SendMessageWithFileToContactJid` **update** New parameter Geolocation (can be null)
- `SendMessageWithFileToConversation` **update** New parameter Geolocation (can be null)
- `SendMessageWithFileToConversationId` **update** New parameter Geolocation (can be null)

***Message object***: **update**
- `Geolocation` **new** A geodetic datum and a set of reference points.

***Administration service***: **new**
- `AddUserToOtherUserNetwork`: To add a user to other user network. So both share their presences.
- `AskAuthenticationTokenOnBehalf`: To get authentication token on behalf  a user
- `CreateAnonymousGuestUser`: To create anonymous guest user
- `CreateCompany`: To create a company
- `CreateGuestUser`: To create anonymous user
- `CreateUser`: To create a user
- `DeleteCompany`: To delete a company
- `DeleteUser`: To delete a user
- `GetCompanies`: To get companies list
- `GetCompany`: To get company details
- `GetUser`: To get user details
- `GetUsers`: To get users list
- `InviteUserInCompany`: To invite an user in a company
- `UpdateCompany`: To update a company
- `UpdatePassword`: To update an user password
- `UpdateUser`: To update an user

***Bubble object***: **update**
- `CustomData` **new** Custom data of the bubble. Key/value format

***Bubbles service***: **update**
- `BubbleCustomDataUpdated` **new** Event fired when custom data of a bubble has been updated / deleted
- `DeleteCustomData` **new** Delete custom data of the bubble
- `UpdateCustomData` **new** Add / Update custom data of the bubble

***Contacts service***: **update**
- `GetCompany` **new** Get Company using company Id
- `GetCurrentCompany` **new** Get Company of the current user from server
- `GetCurrentCompanyFromCache` **new** Get Company of the current user from cache

***Telephony service***: **update**
- `ConsultationCall` **new** Method preferably used to make a second call (instead of HoldCall then MakeCall)
- `MakeCall` **update** The callback now returns a Call object (not only the CallId)
- `MakeCallWithSubject` **update** The callback now returns a Call object (not only the CallId)
- `MakeCallToMevo` **update** The callback now returns a Call object (not only the CallId)

***Call object***: **update**
- `GlobalCallId` **new** Global Call Id of the call

***Channels service***: **update**
- `ChannelItemAppreciationsUpdated` **new** Event to know if appreciations on a channel item has changed

## [1.9.0.0] - 2020-03-25
---

***Groups service***: **new**

New service which allow to create / update / delete personal groups

It's possible to add / remove users as members

***Application service***: **update**
- `isCapabilityEnabled` **new** To check if the current user has a capability enabled or not

***Contact.Capability object***: **new**

List all capabilities available (**BubbleCreate**, **BubbleParticipate**,  **FileUpload**, **FileDownload**, **PhoneMeeting** and **UserNameUpdate** are available for the moment)

***Contacts service***: **update**

Update of the current contact, check the new capability **UserNameUpdate** if **Last Name, First Name or Title** is updated

***Bubbles, Conversations, FileStorage, Instant Messaging and Channels services***: **update**

**FileStorage** service, used to download / upload files, checks new capabilities (**FileUpload** and **FileDownload**)

**InstantMessaging / Channels services** are impacted when processing messages / items with attached files. You can always receive messages / items with attachments without the **FileDownload** capability but you can get the files.

**Bubbles, Conversations, InstantMessaging services** are impacted when Bubbles are involved according capabilities **BubbleCreate** and **BubbleParticipate** 

***Contact object***: **update**
- `IsTv` **new** To know if the associated rainbow user is used as a TV Device.
- `Tags` **new** An Array of free tags associated to the rainbow user.

***Contact.PhoneNumber object***: **update**
- `IsVisibleByOthers` **new** To know it the phone numbers must be shared to other users

***Channels service***: **update**
- `LikeItem` **new** To like a Channel Item with an appreciation
- `GetDetailedAppreciations` **new** To know in details appreciations given on a channel item (by userId the appreciation given)

***ChannelItem object***: **update**
- `Appreciations` **new** To know appreciations given on a channel item (count by type of appreciations)
- `MyAppreciation` **new** To know the appreciation given by the current user

***Telephony service***: **update**

Add CCD(Call Center Distribution) features : Logon, Logoff, Withdrawal and WrapUp
 

## [1.8.0.0] - 2020-03-05
---

***Channels service***: **new**

New service which allow to create / update / delete channels (public, private and closed channels for your company)

It's possible to add / update / delete  items in channel (with or without images / attachments)

***InstantMessaging service***:

Allow to delete all messages in a one to one conversation. Messages are deleted only for you. They are NOT deleted for your contact.
- `DeleteAllMessages` **new** (only available in XMPP event mode)

Event raised when all messages are deleted in a one to one conversation. Before this event is raised, messages stored in the cache, for this conversation, are deleted. 
- `MessagesAllDeleted` **new** (available in XMPP and S2S event mode)

## [1.7.0.0] - 2020-02-11
---

***Server to Server (S2S) event mode and XMPP event mode***

It's the first SDK which allow you to switch easily between XMPP event mode (standard usage - desktop/mobile applications or small bots) and S2S event mode (Useful for creating advanced bots or high availability and scalable server applications). 

Same code than before can be used - only a property must be set to switch between event mode.

In S2S event mode a callback URL (also called Webhook) must be defined and you must use a web server.

Full documentation available [here](https://hub.openrainbow.com/#/documentation/doc/sdk/csharp/guides/035_events_mode) 

Restriction of use available [here](https://hub.openrainbow.com/#/documentation/doc/sdk/csharp/guides/025_methods_and_events_available) and [here](https://hub.openrainbow.com/#/documentation/doc/sdk/csharp/guides/038_restriction_of_use).

Example available [here](https://github.com/Rainbow-CPaaS/Rainbow-CSharp-SDK-Samples/tree/master/S2S)  

***Message storage mode***:

It's now possible to use 3 different storage mode for messages sent (2 modes only in previous versions).
- `Store`: (Default mode) When a message is sent, it will be stored by the server. So both, sender and receiver will retrieve it as archive even after reconnection.
- `NoPermanentStore`: The server will store the message, for the receiver ONLY, UNTIL he receives it. This message will not be available as archive. Useful in BOT context.
- `NoStore`: The server will not store the message. If the receiver is not connected when the message is sent, he will never receive the message. Useful in BOT context.

To handle this, use `MessageStorageMode` property from `Restrictions` object.

***Application object***:

Allow to set default presence level once connected to the server. Must be used before connecting to the server.
- `SetDefaultPresenceLevel` **new**

***InstantMessaging service***:

Allow sending messages by mentioning users: (only in Bubble context)
- `SendMessageWithFileToBubble` **updated** (by Bubble object, By Id, By Jid) 
- `SendMessageWithFileToConversation` **updated** (by Conversation object, by Id)
- `SendMessageToBubble` **updated** (by Bubble object, By Id, By Jid)
- `SendMessageToConversation` **updated** (by Conversation object, by Id)

Permit to know if current user has been mentioned in a message: (only in Bubble context)
- in `Message` object new properties: `Attention` and `Mentions`

***Bubbles service***: 
- `GetAllBubbles` **updated** Now always get all bubbles from server
- `GetAllBubblesFromCache` **new** Get all bubbles from cache

***Contact object***:
- `LoginEmail` **new**

***Conversations service***:
- `GetConversationByJidFromCache` **new**
- `GetConversationByPeerIdFromCache` **new**
- `GetConversationIdByJidFromCache` **new**

## [1.6.1.0] - 2020-01-29
---

- **Presence**: Fix Aggregation Presence - Manual Away was badly managed  

## [1.6.0.0] - 2020-01-23
---

- **MAJOR UPDATE**: Presence - It's now possible to get Presences list of all resources used for a specific Contact and an aggregated Presence which aggregate the Presences List in only one Presence Level. (following rules from “Rainbow presence - v2.ppt” specifications)  
- **UPDATE**: Mobile Application - Allow to set the SDK as "mobile application" to have correct Presence Level displayed in others Applications/SDK (Using Applications.Restrictions.MobileApplication) 

## [1.5.0.0] - 2020-01-06
---

- **MAJOR UPDATE**: When the user is connected, the SDK now automatically get **ONLY Contacts** . Conversations, Favorites, FileDescriptor of sent/received files are **NO MORE** automatically get.
- **MAJOR UPDATE**: Add property **InRoster** in **Contact** model class. Add **RosterContactAdded** and **RosterContactRemoved** events in **Contacts** class. **ContactRemoved** event no more exists. **GetAllContacts** and **GetAllContactsInRoster** get only data from the cache. Add method **GetAllContactsInRosterFromServer**
- **MAJOR UPDATE**: A new **Restrictions** class has been added accessible using **Application.Restrictions** property. It permits to set SDK restrictions. Very usefull if are using this SDK in BOT context to increase performance.
- **UPDATE**: For coherency, when sending message in one to one conversation (**SendMessageToConversationId**, **SendMessageWithFileToConversationId**, **SendAlternativeContentsToConversationId**, etc ...), **MessageReceived** event is now raised. In conversation Bubble, it was already the case.
- **FIX**: **ConversationUpdated** event is raised when a message is set as read by the server to know the correct number of unread message.
- **FIX**: **ContactAdded** event is raised when **GetContactFromContactIdFromServer** and **GetContactFromContactJidFromServer** methods from **Contacts** class is used and when the contact found is not already in cache. 
- **FIX**: Incorrect parsing of Message describing a CallLog in WebRTC
- **FIX**: When a IM is received as an edited message context (replace), the event **ConversationUpdated** from **Conversations** could be badly raised. This event is now raised only if the edited message was previoulsy the last message in the conversation.
- **FIX**: Messages edited (replaced) are now correctly managed when getting older messages from server or when receiving new messages 
- **FIX**: Ask correctly the server to find a specific older message in a Bubble context

## [1.4.0.0] - 2019-11-14
---

- **FIX**: When a new IM is received, the event **ConversationUpdated** from **Conversations** class is now raised.
- **FIX**: It was not possible to set Presence Level to Busy with details set to Audio, Video or Sharing
- **FIX**: **BubbleAvatarUpdated** event from **Bubbles** class should not be raised when the avatar is deleted
- RQRAINB-2578: Add 3 samples for MacOS: Contacts, Conversations/Favorites and Instant Messaging
- RQRAINB-2579: Hub - Documentation: Add guide about MacOs environment installation to use CSharp SDK

## [1.3.0.0] - 2019-10-25
---

- RQRAINB-2417: Update log management to support multi-platform (Windows, MacOs, IOs, Android)
- RQRAINB-2418: Update all samples to use new log management
- RQRAINB-2419: Bubbles - Add Avatar management (create / delete / update)
- RQRAINB-2420: Bubbles - Add public URL management (create / regenerate / delete)

## [1.2.0.0] - 2019-10-04
---

- RQRAINB-2279: Manage Conferences (PSTN and WebRTC) - API
- RQRAINB-2280: Manage Conferences (PSTN and WebRTC) - Documentation
- RQRAINB-2281: Manage Conferences (PSTN and WebRTC) - Sample
- RQRAINB-2293: Hub - Documentation: Add guide about Bubbles management
- RQRAINB-2294: Hub - Documentation: Add guide about files management
- RQRAINB-2295: Hub - Documentation: Add guide about conferences management (PSTN and WebRTC)
- RQRAINB-2296: Hub - Documentation: Add guide about telephony management (3PCC, Voice mail, Nomadic, Call Fwd)

## [1.1.0.0] - 2019-09-13
---

- RQRAINB-2187: Add new Sample about Telephony features
- RQRAINB-2188: Allow inline documentation of the CSharp SDK
- RQRAINB-2189: Enhance documentation to allow to link between pages

## [1.0.0.0] - 2019-08-14
---

- RQRAINB-1889: Search for contacts by tag
- RQRAINB-1953: FileStorage Service - Documentation
- RQRAINB-1954: FileStorage Service - Module
- RQRAINB-1955: IM Advanced Features - Module
- RQRAINB-1956: IM Advanced Features - Documentation
- RQRAINB-2008: IM Advanced Features - Send alternative content

## [0.1.0.0] - 2019-07-24
---

- RQRAINB-1826: HDS: Logs cleanup
- RQRAINB-1708: 3PCC Service - Module
- RQRAINB-1711: 3PCC Service - Documentation
- RQRAINB-1872: Voice messages - Module
- RQRAINB-1873: Voice messages - Documentation
- RQRAINB-1883: FileStorage Service - Mini-module for voice message only - Module
- RQRAINB-1882: FileStorage Service - Mini-module for voice message only - Documentation

## [0.0.0.7] - 2019-07-03
---

- RQRAINB-1709: Calls Log Service - Module
- RQRAINB-1710: Calls Log Service - Documentation
- RQRAINB-1713: User Profile / User Features - Internally managed in SDK CSharp
- RQRAINB-1716: Fwd / Nomadic Status - Module
- RQRAINB-1717: Fwd / Nomadic Status - Documentation
- RQRAINB-1718: Phone / Calendar Presence (with documentation)
- RQRAINB-1720: "SearchContactByPhoneNumber" in RB/ Phonebook / O365 AD
- RQRAINB-1727: "SearchContactsByDisplayName" - in PhoneBook and O365 AD
- RQRAINB-1741: Update basic guide on Hub to add references to CSharp SDK
- RQRAINB-1743: "SearchContactsByDisplayName" / "SearchContactsByPhoneNumber" - in Company Directory
