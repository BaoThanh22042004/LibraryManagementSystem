# Library Management System - Entity Relationship Diagram

![Entity Relationship Diagram](https://uml.planttext.com/plantuml/svg/fLPDRvj04Br7odz0EPT3xr4KmBQ2Oh25SNfUmyJU6bYq7sdTgl-zin-0NL0KfxUtoywyzsQyituKad1P7A3nbdJ72J_DizdjpUqDD9BAatUt5S35dKU4j_L-tzvuNbnwz_T7E3qywDL2LLLAQj1uNXDQQR0cGlnWl7mYug3N6QlCWQ0iEGXXZXnOO_OsaaXbjXOO3UMIxMaJI0brAytsaXL7fkIyANDgUTRGbBJPztGH1oAX3AGvJeHCM4bVgTtvSsucK2MLo60D1SifHq4XAMloKsizcSZBLLmEkFC-0ubgZE0GNXa7xSkQVfN0-rNSdXK1TIfRcLXiL56uxJddZ2Uu87kupb_8sD5w2wUivLIwHYXvODmSs8IfgQdQLLGSWFUBWkWwp9pkeKqHUmCUrwXjoojhNHISjlhqzMeZrb9magCfpNZWbAfwPvMToVWmJOJdzentD9CiGnillVphzKfpbldax0MvfJPTClphvo-BP-UpLtdvvI9f0heGraSovSCTED1sgDcmjz5tlUlenROhwaf9d0VcTYre0rSMsWuGIXehSF9spUksOYS0UuUUA8TEpnKyhATOJN7exrtMcUe9Cf2AJ_sWFqniQcA85vFNoqs9e6QgCTbqm9ZmqnSc0np6R-PUMXdPb8opK_zZDcKIFnHdREbxfZTgznqAQNzP_UWQY3UOrxA-Aq7B9sCnPjQ_0EyLvUm8JjRy9PyKfa-Okmq_-gTYLPKzdlziyUSdB6iiyA4hhwjGzypfWiipGUliVK6V9kPGsNRWfVNkMk3uqDHG-ZixOJ8pzF4a0Z_ryNspyAllRSVX1oBy0eUh2NsAN2W2VJwq29E4qJYqSBFg5t8JDrNjSCCC6haK1cAEYHGxg5Yp5xvaTZXsqM7UwGrjB7Pt7CrrnONVkWRuM0TRWNLNWNMNHYDCWpqPfw72hyqKDC5nue8Hk627ad4eqXv5P_vRu28Hk4Y7bkD8IOvkYEJFBWo1et5egkYqAPGR_N7QjKiZmxCQS3B0nJXDhUbzFim8-vXaBY42byEXS42HXLZDRAeb1T0tvooBNhf68EeweI46fphu4V_3wN-TVm40 "Entity Relationship Diagram")

```
@startchen LibraryERD

entity "Users" as U {
  Id <<key>>
  FullName
  Email
  PasswordHash
  Role
  Address
  Phone
  Status
  FailedLoginAttempts
  LockoutEndTime
  PendingEmail
  CreatedAt
  LastModifiedAt
}

entity "AuditLogs" as AL {
  Id <<key>>
  ActionType
  EntityType
  EntityId
  EntityName
  Details
  BeforeState
  AfterState
  IpAddress
  Module
  IsSuccess
  ErrorMessage
  CreatedAt
  LastModifiedAt
}

entity "Books" as B {
  Id <<key>>
  Title
  Author
  ISBN
  Publisher
  PublicationDate
  Status
  CoverImageUrl
  Description
  CreatedAt
  LastModifiedAt
}

entity "BookCopies" as BC {
  Id <<key>>
  CopyNumber
  Status
  CreatedAt
  LastModifiedAt
}

entity "Categories" as C {
  Id <<key>>
  Name
  Description
  CoverImageUrl
  CreatedAt
  LastModifiedAt
}

entity "BookCategory" as BCAT {
  BooksId <<PK,FK>>
  CategoriesId <<PK,FK>>
}

entity "Members" as M {
  Id <<key>>
  MembershipNumber
  MembershipStartDate
  MembershipStatus
  OutstandingFines
  CreatedAt
  LastModifiedAt
}

entity "Librarians" as L {
  Id <<key>>
  EmployeeId
  HireDate
  CreatedAt
  LastModifiedAt
}

entity "Loans" as LO {
  Id <<key>>
  LoanDate
  DueDate
  ReturnDate
  Status
  CreatedAt
  LastModifiedAt
}

entity "Fines" as F {
  Id <<key>>
  Type
  Amount
  FineDate
  Status
  Description
  CreatedAt
  LastModifiedAt
}

entity "Reservations" as R {
  Id <<key>>
  ReservationDate
  Status
  CreatedAt
  LastModifiedAt
}

entity "Notifications" as N {
  Id <<key>>
  Type
  Subject
  Message
  Status
  SentAt
  CreatedAt
  LastModifiedAt
  ReadAt
}

entity "EmailVerificationTokens" as EVT {
  Id <<key>>
  Token
  NewEmail
  OldEmail
  ExpiresAt
  IsUsed
  CreatedAt
  LastModifiedAt
}

entity "PasswordResetTokens" as PRT {
  Id <<key>>
  Token
  ExpiresAt
  IsUsed
  CreatedAt
  LastModifiedAt
}

relationship "performed-by" as RB {
}
RB -N- AL
RB -1- U

relationship "has-copy" as HC {
}
HC -1- B
HC -N- BC

relationship "categorized-as" as CAT {
}
CAT -N- B
CAT -N- C

relationship "belongs-to" as BT {
}
BT -1- U
BT -N- EVT

relationship "resets-password-for" as RP {
}
RP -1- U
RP -N- PRT

relationship "is-member" as IM {
}
IM -1- U
IM -1- M

relationship "works-as" as WA {
}
WA -1- U
WA -1- L

relationship "takes" as TK {
}
TK -1- M
TK -1- BC

relationship "incurs" as INC {
}
INC -1- LO
INC -1- M
INC -1- F

relationship "makes-reservation" as MR {
}
MR -1- M
MR -1- B
MR -1- BC
MR -N- R

relationship "receives" as RCV {
}
RCV -1- U
RCV -N- N

@endchen
```