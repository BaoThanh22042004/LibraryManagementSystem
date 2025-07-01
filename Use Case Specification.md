# Library Management System - Use Case Specification

## Use Cases

### User Management Use Cases

#### UC001: Create User

**Use Case ID and Name:** UC001 - Create User

**Author and Date Created:** Technical Lead, June 25, 2025

**Primary Actor:** Admin, Librarian

**Secondary Actors:** System

**Trigger:** Admin or Librarian needs to add a new user to the system

**Description:** This use case allows authorized staff members to create new user accounts based on their role permissions. Librarians can create new Member accounts for library patrons. Administrators can create both Librarian accounts for staff and Member accounts for patrons. The system captures essential user information and assigns appropriate access permissions based on the user's role. For Member accounts, the system also creates a library membership record with borrowing privileges.

**Preconditions:**
- PRE-1: Actor must be logged into the system
- PRE-2: Actor must have user management permissions
- PRE-3: System must be operational

**Postconditions:**
- POST-1: New user account is created with unique identifier
- POST-2: User credentials are securely stored in the system
- POST-3: User role and permissions are properly assigned
- POST-4: For Member accounts, membership record is created with unique membership number
- POST-5: System confirms successful account creation

**Normal Flow (1.0):**
1. Actor selects "Create New User" from the user management menu
2. System displays user registration form
3. Actor enters required information (full name, email address, password, user role)
4. Actor submits the form
5. System validates all input data for completeness and format
6. System verifies that email address is not already in use
7. System checks that password meets security requirements
8. System checks if actor has permission to create the specified user role
9. If creating Member account and membership number is provided, system validates membership number format
10. System creates the user account with assigned role
11. If role is "Member", system creates membership record with unique membership number
12. System saves the new user information
13. System displays confirmation message with new user details

**Alternative Flows:**
- 1.1: Create Library Member Account (Librarian or Admin)
  - 3a. Actor selects "Member" role
  - 3b. Actor optionally enters preferred membership number
  - 9a. System creates both user account and library membership record
  - 9b. System generates unique membership number if not provided
  - 9c. System sets membership status to Active and start date to current date
- 1.2: Create Librarian Account (Admin only)
  - 3a. Admin selects "Librarian" role and enters employee information
  - 9a. System creates user account with staff privileges
  - 9b. System records employment start date

**Exceptions:**
- 1.0.E1: Duplicate Email Address
  - 6a. System finds existing account with same email
  - 6b. System displays error message "This email address is already registered"
  - 6c. Actor must use different email address
- 1.0.E2: Invalid Information Format
  - 5a. System detects missing required fields, invalid email format, or phone number format
  - 5b. System highlights problematic fields with specific validation messages
  - 5c. Actor must correct all validation errors before resubmitting
- 1.0.E3: Weak Password
  - 7a. Password does not meet security standards
  - 7b. System displays password requirements and examples
  - 7c. Actor creates stronger password
- 1.0.E4: Duplicate Membership Number
  - 9a. System detects duplicate membership number (if provided)
  - 9b. System displays error "Membership number already exists"
  - 9c. Actor must provide different membership number or let system auto-generate
- 1.0.E5: Insufficient Role Permissions
  - 8a. Actor attempts to create user role beyond their authorization (e.g., Librarian trying to create Admin)
  - 8b. System displays "Insufficient permissions to create this user role" error
  - 8c. Actor must contact administrator or select appropriate role

**Priority:** High

**Frequency of Use:** 20-50 times per week

**Business Rules:** BR-01, BR-02, BR-05, BR-22

**Other Information:**
- User roles include Member, Librarian, and Administrator
- Password security standards are enforced automatically
- Email addresses must be unique across all users
- For Member accounts, membership numbers can include letters, numbers, and hyphens
- Maximum membership number length is 20 characters
- System can auto-generate membership numbers if not provided
- Account activation may be immediate or require email verification

**Assumptions:**
- Staff members understand different user role permissions
- Users will have access to the email address provided
- Password requirements are clearly communicated to users
- For Member accounts, user information is accurate and current

---

#### UC002: Login

**Use Case ID and Name:** UC002 - Login

**Author and Date Created:** Technical Lead, June 25, 2025

**Primary Actor:** Member, Librarian, Admin

**Secondary Actors:** System

**Trigger:** User attempts to access the library system

**Description:** This use case enables users to log into the library system using their email and password credentials. Upon successful authentication, users receive appropriate access to system features based on their assigned role.

**Preconditions:**
- PRE-1: User must have a valid account in the system
- PRE-2: System authentication must be operational
- PRE-3: User must know their login credentials

**Postconditions:**
- POST-1: User is successfully logged into the system
- POST-2: User access is established with appropriate permissions
- POST-3: User is directed to their role-specific dashboard
- POST-4: Login activity is recorded for security purposes

**Normal Flow (2.0):**
1. User accesses the library system login page
2. System displays login form requesting email and password
3. User enters their email address and password
4. User clicks the "Login" button
5. System validates that both fields are completed
6. System checks account status (active, suspended, expired)
7. System verifies the email exists in the user records
8. System confirms the password matches the stored credentials
9. System records failed login attempts for security monitoring
10. System establishes user access with appropriate role permissions
11. System clears any previous failed login attempts upon successful authentication
12. System redirects user to their role-appropriate homepage
13. User gains access to authorized system features

**Alternative Flows:**
- 2.1: Remember Login Option
  - 3a. User selects "Remember Me" checkbox before logging in
  - 10a. System extends access duration for user convenience
- 2.2: Role-Based Dashboard Access
  - 12a. Members are directed to the member portal
  - 12b. Librarians are directed to the staff interface
  - 12c. Administrators are directed to the admin control panel
- 2.3: Account Security Lockout
  - 9a. System detects multiple failed login attempts (configurable limit: 5 attempts)
  - 9b. System temporarily locks account for security (lockout duration: 15 minutes)
  - 9c. System displays "Account temporarily locked due to multiple failed attempts"

**Exceptions:**
- 2.0.E1: Invalid Login Credentials
  - 7a. System cannot find user with provided email
  - 7b. OR password does not match stored credentials
  - 7c. System displays generic error "Invalid email or password"
  - 7d. User must re-enter correct credentials
- 2.0.E2: Missing Login Information
  - 5a. User leaves email or password field empty
  - 5b. System displays validation message for required fields
  - 5c. User must complete all required fields
- 2.0.E3: System Authentication Error
  - 10a. System encounters technical error during login process
  - 10b. System displays "Login temporarily unavailable" message
  - 10c. User should try again or contact support if problem persists
- 2.0.E4: Account Status Inactive
  - 6a. System detects user account is suspended or expired
  - 6b. System displays appropriate status message
  - 6c. User must contact administrator to resolve account status

**Priority:** High

**Frequency of Use:** 200-500 times per day

**Business Rules:** BR-05, BR-24

**Other Information:**
- System uses secure authentication methods to protect user credentials
- Login access has configurable timeout periods for security
- Failed login attempts may be limited to prevent unauthorized access
- Users can reset forgotten passwords through email verification

**Assumptions:**
- Users remember their registered email address and password
- Users have access to a web browser and internet connection
- System maintains reliable authentication availability

---

#### UC003: Update Profile

**Use Case ID and Name:** UC003 - Update Profile

**Author and Date Created:** Technical Lead, June 25, 2025

**Primary Actor:** Member, Librarian, Admin

**Secondary Actors:** System

**Trigger:** User needs to modify their own profile information

**Description:** This use case allows users to update their own personal information including name, email address, phone number, and address. Users have full control over their basic profile information but cannot modify system-assigned data like user ID, role, or membership status.

**Preconditions:**
- PRE-1: User must be logged into the system
- PRE-2: User is updating their own profile information

**Postconditions:**
- POST-1: User's own profile information is updated successfully
- POST-2: Changes are saved and immediately available in the system
- POST-3: User receives confirmation of successful update
- POST-4: Profile modification is recorded for audit purposes

**Normal Flow (3.0):**
1. User accesses their own profile update page via "My Profile" menu
2. System displays current user information in editable form
3. User modifies desired fields (name, email address, phone number, address)
4. User reviews changes and clicks "Save Changes"
5. System validates all modified information for correctness
6. System validates phone number format if provided
7. System verifies that email address is unique if changed
8. System saves the updated information
9. System sends email verification if email address was changed
10. System displays confirmation message
11. Updated information is immediately available throughout the system

**Alternative Flows:**
- 3.1: Self-Profile Update Only
  - 1a. User can only access their own profile from the account menu
  - 3a. User can modify personal information (name, email, contact details) but not role or permissions

**Exceptions:**
- 3.0.E1: Email Already in Use
  - 7a. System finds another user with the same email address
  - 7b. System displays "Email address already in use" error
  - 7c. User must choose a different email address
- 3.0.E2: Invalid Information Format
  - 5a. System detects incorrectly formatted data (invalid email, phone number, empty required fields)
  - 5b. System highlights problematic fields with specific error messages
  - 5c. User corrects the invalid information
- 3.0.E3: Email Verification Required
  - 9a. User changes email address to new one
  - 9b. System sends verification email to new address
  - 9c. System keeps old email active until verification is completed
  - 9d. User must verify new email to complete profile update

**Priority:** Medium

**Frequency of Use:** 20-50 times per week

**Business Rules:** BR-01, BR-03, BR-04

**Other Information:**
- Email addresses must remain unique across all user accounts
- Users can only modify their own basic profile information (name, email, contact details)
- Role changes are not allowed through this interface
- All profile changes take effect immediately upon saving
- System maintains audit trail of all profile changes

**Assumptions:**
- Users understand their permission limitations
- Email format validation follows standard conventions
- System maintains data consistency during profile updates

---

#### UC004: Change Password

**Use Case ID and Name:** UC004 - Change Password

**Author and Date Created:** Technical Lead, June 25, 2025

**Primary Actor:** Member, Librarian, Admin

**Secondary Actors:** System

**Trigger:** User needs to change their password for security reasons or regular maintenance

**Description:** This use case allows authenticated users to securely change their account password. Users must provide their current password for verification and create a new password that meets the system's security requirements.

**Preconditions:**
- PRE-1: User must be logged into the system
- PRE-2: User must know their current password
- PRE-3: System password reset functionality must be operational

**Postconditions:**
- POST-1: User password is successfully updated with new secure password
- POST-2: Password change is recorded in system logs
- POST-3: User receives confirmation of successful password change
- POST-4: User can immediately use new password for future logins

**Normal Flow (4.0):**
1. User accesses password change page from their profile menu
2. System displays password change form
3. User enters current password in the "Current Password" field (hidden input with show/hide toggle)
4. User enters new password in the "New Password" field with real-time strength indicator
5. User re-enters new password in the "Confirm New Password" field
6. User clicks "Change Password" button
7. System verifies that all fields are completed
8. System confirms that new password matches confirmation password
9. System validates that current password is correct
10. System checks that new password meets security requirements
11. System updates the user's password
12. System invalidates all existing user sessions except current one
13. System displays success confirmation message

**Alternative Flows:**
- 4.1: First-Time Password Change
  - 9a. User has temporary password that bypasses current password verification
  - 11a. System clears temporary password status after successful change
- 4.2: Administrator Password Reset
  - 1a. Administrator accesses user management to reset another user's password
  - 9a. Administrator can bypass current password verification for target user

**Exceptions:**
- 4.0.E1: Current Password Incorrect
  - 9a. System determines provided current password is wrong
  - 9b. System displays "Current password is incorrect" error message
  - 9c. User must enter correct current password
- 4.0.E2: Password Confirmation Mismatch
  - 8a. New password and confirmation password do not match
  - 8b. System displays "New password and confirmation do not match" error
  - 8c. User must re-enter matching passwords
- 4.0.E3: Password Does Not Meet Requirements
  - 10a. New password fails to meet security standards
  - 10b. System displays specific requirements that were not met
  - 10c. User must create password meeting all requirements
- 4.0.E4: User Account Not Found
  - 9a. System cannot locate user account during password verification
  - 9b. System displays "Account error, please try logging in again"
  - 9c. User may need to re-authenticate

**Priority:** High

**Frequency of Use:** 20-50 times per week

**Business Rules:** BR-05, BR-22

**Other Information:**
- Password requirements include minimum length, character variety, and complexity
- Current password verification prevents unauthorized password changes
- New passwords are immediately effective after successful change
- System provides clear guidance on password security requirements

**Assumptions:**
- Users remember their current password
- Password requirements are clearly communicated and understood
- System maintains secure password storage and verification methods

---

#### UC005: Reset Password

**Use Case ID and Name:** UC005 - Reset Password

**Author and Date Created:** Technical Lead, June 25, 2025

**Primary Actor:** Member, Librarian, Admin

**Secondary Actors:** System, Email Service

**Trigger:** User has forgotten their password and needs to reset it to regain access to their account

**Description:** This use case allows users to securely reset their forgotten password through email verification. Users provide their registered email address, receive a secure reset link, and create a new password to regain account access.

**Preconditions:**
- PRE-1: User must have a valid account in the system
- PRE-2: User must have access to their registered email address
- PRE-3: Email service must be operational
- PRE-4: Password reset functionality must be enabled

**Postconditions:**
- POST-1: User password is successfully reset with new secure password
- POST-2: Password reset is recorded in system logs for security purposes
- POST-3: Previous password is invalidated and cannot be used
- POST-4: User can immediately login with new password
- POST-5: Reset link is deactivated after successful use

**Normal Flow (5.0):**
1. User accesses the login page and clicks "Forgot Password" link
2. System displays password reset request form
3. User enters their registered email address
4. User clicks "Send Reset Link" button
5. System validates email format and existence in user records
6. System performs rate limiting check (max 3 reset requests per hour per email)
7. System generates secure, time-limited reset token
8. System sends password reset email with secure link to user's email
9. System displays confirmation message about email being sent
10. User checks email and clicks the reset link
11. System validates the reset token and expiration time
12. System displays new password creation form
13. User enters new password and confirmation
14. System validates new password meets security requirements
15. System updates user password and invalidates reset token
16. System invalidates all existing user sessions for security
17. System sends confirmation email about successful password reset
18. System displays success message and redirects to login page

**Alternative Flows:**
- 5.1: Reset Link Expired
  - 10a. User clicks reset link after expiration time
  - 10b. System displays "Reset link expired" message
  - 10c. System prompts user to request new reset link
- 5.2: Multiple Reset Requests
  - 6a. User requests multiple reset links before using first one
  - 6b. System invalidates previous reset tokens
  - 6c. System sends new reset link and only the latest is valid

**Exceptions:**
- 5.0.E1: Email Not Found
  - 5a. System cannot find user account with provided email
  - 5b. System displays generic message "If email exists, reset link will be sent"
  - 5c. System does not reveal whether email exists for security reasons
- 5.0.E2: Invalid Email Format
  - 5a. User enters incorrectly formatted email address
  - 5b. System displays "Please enter valid email address" error
  - 5c. User must correct email format before proceeding
- 5.0.E3: Email Service Unavailable
  - 8a. System cannot send reset email due to service failure
  - 8b. System displays "Email service temporarily unavailable" message
  - 8c. System suggests trying again later or contacting support
- 5.0.E4: Invalid Reset Token
  - 11a. System detects tampered or invalid reset token
  - 11b. System displays "Invalid reset link" error message
  - 11c. System prompts user to request new reset link
- 5.0.E5: Password Does Not Meet Requirements
  - 14a. New password fails to meet security standards
  - 14b. System displays specific requirements that were not met
  - 14c. User must create password meeting all requirements
- 5.0.E6: Rate Limit Exceeded
  - 6a. User has exceeded maximum reset requests within time window
  - 6b. System displays "Too many reset requests. Please try again in X minutes"
  - 6c. User must wait before making additional reset requests

**Priority:** High

**Frequency of Use:** 50-150 times per month

**Business Rules:** BR-05, BR-22, BR-24, BR-26

**Other Information:**
- Reset links expire after configurable time period (typically 1 hour)
- Only one reset token is valid per user at any time
- System maintains security by not revealing whether email addresses exist
- Reset process is logged for security monitoring and audit purposes
- Email includes clear instructions and security warnings
- Reset tokens are cryptographically secure and single-use

**Assumptions:**
- Users have access to their registered email address
- Email service reliably delivers reset emails
- Users understand email security and will not share reset links
- Reset links are processed within expiration timeframe

---


#### UC006: Update User Info

**Use Case ID and Name:** UC006 - Update User Info

**Author and Date Created:** Technical Lead, June 25, 2025

**Primary Actor:** Librarian, Admin

**Secondary Actors:** System

**Trigger:** Staff member needs to update user information or user status based on their authority level

**Description:** This use case allows authorized library staff to update user information and status based on their role permissions. Librarians can update Member information and status only. Administrators can update both Librarian and Member information and status. This includes personal details, contact information, membership/employment status, and account restrictions.

**Preconditions:**
- PRE-1: Staff member must be logged into the system
- PRE-2: Staff member must have appropriate user management permissions
- PRE-3: Target user account must exist in the system
- PRE-4: Staff member must have authorization to modify the selected user type

**Postconditions:**
- POST-1: User information and/or status is updated in the system
- POST-2: Changes are saved and immediately available throughout the system
- POST-3: Updated information is reflected in all related system functions
- POST-4: Status change is recorded with date, time, and modifier for audit purposes
- POST-5: User receives notification of significant changes if appropriate

**Normal Flow (6.0):**
1. Staff member accesses user management page
2. System displays user search and management functionality
3. Staff member searches for and selects target user
4. System displays current user information and available update options
5. Staff member selects information or status to update
6. System displays update form with current values
7. Staff member modifies desired fields (personal info, contact details, status, account restrictions, borrowing limits)
8. Staff member validates that status changes follow proper workflow
9. Staff member provides reason for changes if required (especially for status changes)
10. Staff member confirms the updates
11. System validates all changes and permissions
12. System sends notification to user if significant changes are made
13. System saves updated information
14. System displays confirmation message with updated details

**Alternative Flows:**
- 6.1: Update Personal Information (Librarian for Members, Admin for Librarians/Members)
  - 7a. Staff member updates name, email, phone, address, or other contact information
  - 11a. System validates email uniqueness and information format
  - 13a. System updates user record immediately
- 6.2: Update User Status (Librarian for Members, Admin for Librarians/Members)
  - 7a. Staff member changes user status (Active, Suspended, Expired for Members; Active, Inactive for Librarians)
  - 9a. Staff member provides mandatory reason for status change
  - 13a. System updates status and records reason with timestamp
- 6.3: Combined Information and Status Update
  - 7a. Staff member updates both personal information and status in single transaction
  - 13a. System processes all changes together ensuring data consistency
- 6.4: Update Account Restrictions
  - 7a. Staff member modifies borrowing limits or account restrictions
  - 9a. Staff member provides reason for restriction changes
  - 13a. System applies restrictions immediately and logs reason

**Exceptions:**
- 6.0.E1: User Not Found
  - 3a. System cannot locate user with specified identifier
  - 3b. System displays "User not found" error message
  - 3c. Staff member must provide valid user identifier
- 6.0.E2: Insufficient Permissions
  - 11a. Staff member attempts to modify user type beyond their authorization
  - 11b. System displays "Insufficient permissions" error
  - 11c. System specifies what user types the staff member can modify
- 6.0.E3: Email Already in Use
  - 11a. System detects email address already exists for another user
  - 11b. System displays "Email address already in use" error
  - 11c. Staff member must provide unique email address
- 6.0.E4: Invalid Information Format
  - 11a. System detects incorrectly formatted data (invalid email, phone, etc.)
  - 11b. System highlights problematic fields with specific error messages
  - 11c. Staff member corrects the invalid information
- 6.0.E5: Invalid Status Transition
  - 11a. System detects invalid status change (e.g., suspended to expired without intermediate active state)
  - 11b. System displays valid status transition options
  - 11c. Staff member selects appropriate status change
- 6.0.E6: User Currently Has Active Sessions
  - 12a. System detects user has active login sessions during sensitive updates
  - 12b. System prompts staff to force logout user for security
  - 12c. Staff can proceed with update and automatic session termination

**Priority:** High

**Frequency of Use:** 100-400 times per month

**Business Rules:** BR-01, BR-03, BR-04, BR-22

**Other Information:**
- Librarians can update Member information and status only
- Administrators can update both Librarian and Member information and status
- Email addresses must be unique across all user accounts
- Status changes require documented reasons for audit compliance
- System maintains complete audit trail of all user information changes
- Significant changes may trigger automatic notifications to affected users
- Updates take effect immediately upon saving
- System validates all changes against business rules before saving

**Assumptions:**
- Staff have access to accurate updated information
- Staff understand their permission limitations regarding user types
- User information changes are authorized and legitimate
- System handles concurrent updates appropriately
- Staff document status changes appropriately for audit purposes

---

#### UC007: View User Info

**Use Case ID and Name:** UC007 - View User Info

**Author and Date Created:** Technical Lead, June 25, 2025

**Primary Actor:** Member, Librarian, Admin

**Secondary Actors:** System

**Trigger:** User needs to access user account information and activity history

**Description:** This use case provides access to user account details including personal information, role, account status, and activity history. The information displayed varies based on the user's role and permissions. Members can view their own information, Librarians can view their own and Member information, and Admins can view all user types' information.

**Preconditions:**
- PRE-1: User must be logged into the system
- PRE-2: Target user account must exist in the system
- PRE-3: User must have appropriate permissions to view the requested user information
- PRE-4: Users can view their own information; staff access is role-based

**Postconditions:**
- POST-1: User information is displayed according to user permissions
- POST-2: Information access is recorded for security purposes
- POST-3: Current account status and details are shown accurately
- POST-4: Historical data is presented in organized, easy-to-read format

**Normal Flow (7.0):**
1. User accesses user information page
2. System displays user search options (or loads current user for self-access)
3. User selects target user (or system automatically loads for self-view)
4. System verifies user's permission to view the selected user's information
5. System retrieves comprehensive user data
6. System organizes information based on user role and permissions
7. System displays user summary with key information
8. System provides navigation options for detailed sections
9. User can navigate through different information sections
10. System displays requested detailed information
11. System records the information access for audit purposes

**Alternative Flows:**
- 7.1: Print User Report
  - 8a. User selects print or export option
  - 10a. System provides printable report or downloadable file
- 7.2: Quick User Lookup
  - 2a. Staff uses quick search with user ID or email
  - 7a. System shows condensed key information only
- 7.3: User Self-Service View
  - 1a. User accesses "My Account" from their dashboard
  - 6a. System displays user-appropriate interface with role-based data visibility
- 7.4: Role-Based Information Display
  - 6a. Member viewing own info: sees personal details, loan history, reservations, fines
  - 6b. Librarian viewing Member info: sees full Member details plus administrative notes
  - 6c. Admin viewing any user: sees complete information including system metadata

**Exceptions:**
- 7.0.E1: User Not Found
  - 3a. System cannot locate specified user
  - 3b. System displays "User not found" message
  - 3c. User can try different search criteria
- 7.0.E2: Access Denied
  - 4a. System determines user lacks permission to view requested user information
  - 4b. System displays "Access denied" message
  - 4c. System logs unauthorized access attempt
- 7.0.E3: Data Retrieval Error
  - 5a. System encounters error retrieving user data
  - 5b. System displays "Data temporarily unavailable" message
  - 5c. System suggests retrying or contacting support

**Priority:** High

**Frequency of Use:** 300-800 times per day

**Business Rules:** BR-03, BR-04, BR-24

**Other Information:**
- Information display varies by user role:
  - Members: Can view only their own account information
  - Librarians: Can view their own information and all Member accounts
  - Admins: Can view all user types (Members, Librarians, and other Admins)
- Sensitive information is protected based on role permissions
- Real-time data includes current account status and activity
- Historical data may require special access permissions based on user type
- System maintains audit trail of all user information access

**Assumptions:**
- User privacy policies are enforced through role-based access control
- System performance allows quick data retrieval for all user types
- All user-related data is properly linked and accessible
- Interface adapts appropriately to different user roles and permission levels
- Staff understand their access limitations and responsibilities

---

#### UC008: Register Member

**Use Case ID and Name:** UC008 - Register Member

**Author and Date Created:** Technical Lead, June 25, 2025

**Primary Actor:** Guest (Public User/Prospective Member)

**Secondary Actors:** System

**Trigger:** Guest user wants to register for library membership

**Description:** This use case allows prospective members to register themselves for library membership by creating both a user account and member profile in one process. This enables immediate access to library services upon successful registration.

**Preconditions:**
- PRE-1: User must have access to the registration page
- PRE-2: System must support self-registration functionality
- PRE-3: Member registration functionality must be operational

**Postconditions:**
- POST-1: User account is created with Member role
- POST-2: Member record is created with unique membership number
- POST-3: Password is securely stored in the system
- POST-4: Membership status is set to Active
- POST-5: Both user and member records are created successfully

**Normal Flow (8.0):**
1. User accesses member registration page
2. System displays self-registration form
3. User enters personal information (full name, email, password, confirm password)
4. User optionally provides preferred membership number
5. User submits the registration form
6. System validates all input data for completeness and format
7. System checks email uniqueness across all users
8. System validates password meets security requirements
9. System generates membership number if not provided by user
10. System verifies membership number uniqueness
11. System creates user account with Member role
12. System creates member record linked to new user account
13. System sets membership start date to current date
14. System displays success confirmation with membership details

**Alternative Flows:**
- 8.1: Custom Membership Number
  - 4a. User provides preferred membership number
  - 9a. System validates custom number format and uniqueness
  - 9b. If number is already taken, system suggests alternatives or auto-generates

**Exceptions:**
- 8.0.E1: Email Already Exists
  - 7a. System detects duplicate email address
  - 7b. System displays "Email address already registered" error
  - 7c. User must provide different email address
- 8.0.E2: Password Does Not Meet Requirements
  - 8a. Password doesn't meet security standards
  - 8b. System displays detailed password requirements
  - 8c. User must provide compliant password
- 8.0.E3: Membership Number Conflict
  - 10a. System detects membership number already exists
  - 10b. System auto-generates alternative number
  - 10c. System proceeds with generated number
- 8.0.E4: Registration Process Error
  - 11a. System encounters error during account creation
  - 11b. System cancels the registration process
  - 11c. System displays error message and asks user to try again

**Priority:** High

**Frequency of Use:** 50-150 times per week

**Business Rules:** BR-02, BR-05, BR-22

**Other Information:**
- Password requirements include minimum length and character variety
- Membership numbers are generated using standard library conventions
- Registration process ensures data consistency
- Auto-generated membership numbers include unique identifiers to avoid conflicts

**Assumptions:**
- Users understand and accept library terms and conditions
- Email validation ensures valid contact information
- Self-registration feature is enabled in system configuration

---

#### UC009: Delete User

**Use Case ID and Name:** UC009 - Delete User

**Author and Date Created:** Technical Lead, June 25, 2025

**Primary Actor:** Librarian, Admin

**Secondary Actors:** System

**Trigger:** Staff member needs to permanently remove a user account from the system based on their authority level

**Description:** This use case allows authorized library staff to delete user accounts based on their role permissions. Librarians can delete Member accounts after ensuring all library obligations are cleared. Administrators can delete both Librarian and Member accounts. The system verifies appropriate conditions are met before allowing deletion.

**Preconditions:**
- PRE-1: Staff member must be logged into the system
- PRE-2: Staff member must have appropriate user management permissions
- PRE-3: Target user account must exist in the system
- PRE-4: For Members: must have no active loans, reservations, or unpaid fines
- PRE-5: For Librarians: must have no active responsibilities or pending tasks
- PRE-6: Staff member must have authorization to delete the selected user type

**Postconditions:**
- POST-1: User record is permanently removed from the system
- POST-2: All related data is handled according to retention policies
- POST-3: Deletion is recorded in audit trail with deleter identification
- POST-4: System confirms successful deletion
- POST-5: Related system access and permissions are immediately revoked

**Normal Flow (9.0):**
1. Staff member accesses user management page
2. System displays user search functionality with role-based filtering
3. Staff member searches for and selects target user
4. System displays user details and deletion option (if authorized)
5. Staff member selects "Delete User" option
6. System performs pre-deletion validation based on user type
7. System displays confirmation dialog with user details, dependencies, and warnings
8. Staff member reviews information and confirms deletion
9. System permanently removes user record
10. System updates related records according to retention policies
11. System revokes all access and permissions immediately
12. System displays deletion confirmation message

**Alternative Flows:**
- 9.1: Archive Instead of Delete
  - 8a. Staff member selects "Archive User" option instead
  - 9a. System marks user as archived rather than deleting
  - 10a. System retains user data for historical purposes while disabling access
- 9.2: Delete Member (Librarian or Admin)
  - 6a. System checks for active loans, reservations, unpaid fines, and membership obligations
  - 9a. System removes both user account and membership record if all conditions met
- 9.3: Delete Librarian (Admin only)
  - 6a. System checks for active assignments, pending tasks, and administrative responsibilities
  - 9a. System removes user account and employee record if all conditions met

**Exceptions:**
- 9.0.E1: User Not Found
  - 3a. System cannot locate user with specified identifier
  - 3b. System displays "User not found" error message
  - 3c. Staff member must provide valid user identifier
- 9.0.E2: Insufficient Permissions
  - 4a. Staff member attempts to delete user type beyond their authorization
  - 4b. System displays "Insufficient permissions to delete this user type" error
  - 4c. System specifies what user types the staff member can delete
- 9.0.E3: Member Has Active Loans
  - 6a. System detects active loans for the member
  - 6b. System displays "Cannot delete member with active loans" error
  - 6c. Staff member must resolve active loans before deletion
- 9.0.E4: Member Has Active Reservations
  - 6a. System detects active reservations for the member
  - 6b. System displays "Cannot delete member with active reservations" error
  - 6c. Staff member must cancel reservations before deletion
- 9.0.E5: Member Has Unpaid Fines
  - 6a. System detects unpaid fines for the member
  - 6b. System displays "Cannot delete member with unpaid fines" error
  - 6c. Staff member must resolve outstanding fines before deletion
- 9.0.E6: Librarian Has Active Responsibilities
  - 6a. System detects active assignments or pending tasks for librarian
  - 6b. System displays "Cannot delete librarian with active responsibilities" error
  - 6c. Administrator must reassign responsibilities before deletion

**Priority:** Medium

**Frequency of Use:** 5-30 times per week

**Business Rules:** BR-01, BR-22, BR-23

**Other Information:**
- Librarians can delete Member accounts only
- Administrators can delete both Librarian and Member accounts
- Deletion is permanent and cannot be undone
- Users must clear all obligations and responsibilities before deletion
- Related transaction history may be retained for audit purposes
- System provides archive option as alternative to deletion
- All user access is immediately revoked upon deletion

**Assumptions:**
- Staff member understands the permanent nature of deletion and their permission limitations
- All business obligations and responsibilities are properly resolved before deletion
- Data retention policies are clearly defined and followed
- Staff understand which user types they are authorized to delete

---

### Book Management Use Cases

#### UC010: Add Book

**Use Case ID and Name:** UC010 - Add Book

**Author and Date Created:** Technical Lead, June 25, 2025

**Primary Actor:** Librarian, Admin

**Secondary Actors:** System

**Trigger:** Staff member needs to add a new book title to the library catalog

**Description:** This use case enables authorized library staff to add new book titles to the catalog by providing comprehensive book information. The system creates the book record and automatically generates initial physical copies for circulation.

**Preconditions:**
- PRE-1: Staff member must be logged into the system
- PRE-2: Staff member must have book management permissions
- PRE-3: System must be operational
- PRE-4: At least one category must exist in the system

**Postconditions:**
- POST-1: New book record is created with unique identifier
- POST-2: Book information is validated and stored as Available
- POST-3: Initial book copies are automatically created with unique copy numbers
- POST-4: Book-category associations are established if categories selected
- POST-5: Cover image is uploaded if provided
- POST-6: Book addition is recorded for audit purposes

**Normal Flow (10.0):**
1. Staff member accesses book creation page
2. System displays book creation form with required fields and category options
3. Staff member enters book information (title, author, ISBN, publisher, publication date, description)
4. Staff member selects one or more categories from available options
5. Staff member specifies number of initial copies to create (default: 1)
6. Staff member optionally uploads cover image file
7. Staff member submits the form
8. System validates all input data for completeness and format
9. System checks that ISBN is unique in the catalog
10. System validates cover image file format if provided
11. System creates book record and sets status to Available
12. System uploads cover image and stores reference if provided
13. System creates associations with selected categories
14. System automatically generates initial copies with unique copy numbers
15. System saves all changes and displays confirmation with book details

**Alternative Flows:**
- 10.1: Book Creation with Cover Image Upload
  - 6a. Staff member uploads cover image file
  - 10a. System validates image format (common image types accepted)
  - 12a. System stores image and creates reference for the book
- 10.2: Multiple Categories Selection
  - 4a. Staff member selects multiple categories from available options
  - 13a. System creates associations for all selected categories
- 10.3: Bulk Copy Creation
  - 5a. Staff member specifies higher number of initial copies (e.g., 10)
  - 14a. System generates multiple copies with sequential numbering

**Exceptions:**
- 10.0.E1: Duplicate ISBN Found
  - 9a. System detects existing book with same ISBN
  - 9b. System displays error "A book with this ISBN already exists"
  - 9c. Staff member must provide different ISBN or verify book doesn't exist
- 10.0.E2: Invalid Input Data
  - 8a. System validation fails (missing title, author, or invalid formats)
  - 8b. System highlights problematic fields with specific error messages
  - 8c. Staff member corrects the invalid information before resubmitting
- 10.0.E3: Category Loading Error
  - 2a. System fails to load available categories
  - 2b. System displays error message about category loading
  - 2c. Staff member can proceed without categories but book won't be categorized
- 10.0.E4: Image Upload Failure
  - 10a. Cover image upload encounters error
  - 10b. System displays error message about image upload
  - 10c. Staff member can proceed without image or retry upload

**Priority:** High

**Frequency of Use:** 50-200 times per week

**Business Rules:** BR-06, BR-22

**Other Information:**
- ISBN uniqueness is enforced throughout the system
- Book status is automatically set to Available upon creation
- Copy numbers follow standard library format with sequential numbering
- Initial copies are created with Available status for immediate circulation
- Categories can be selected to help organize and categorize books
- Cover images are stored securely and referenced in book records

**Assumptions:**
- Staff member has necessary permissions to create books
- Image upload functionality is operational for cover image storage
- System maintains proper relationships between books and categories
- Category management is available for loading category options

---

#### UC011: Update Book

**Use Case ID and Name:** UC011 - Update Book

**Author and Date Created:** Technical Lead, June 25, 2025

**Primary Actor:** Librarian, Admin

**Secondary Actors:** System

**Trigger:** Staff member needs to modify existing book information for accuracy or updates

**Description:** This use case allows authorized library staff to modify existing book details in the catalog, including basic information, category assignments, and cover images. The system ensures data accuracy and maintains proper record keeping for all changes.

**Preconditions:**
- PRE-1: Staff member must be logged into the system
- PRE-2: Staff member must have book management permissions
- PRE-3: Book must exist in the catalog
- PRE-4: Staff member must have edit permissions for the specific book

**Postconditions:**
- POST-1: Book information is updated in the catalog
- POST-2: All changes are validated and saved properly
- POST-3: Category associations are updated if modified
- POST-4: Cover image is updated if new image is provided
- POST-5: Book modification timestamp is automatically updated
- POST-6: Book modification is recorded for audit purposes

**Normal Flow (11.0):**
1. Staff member accesses book management page
2. System displays book listing with search and pagination options
3. Staff member selects "Edit" option for the desired book
4. System retrieves current book information including categories
5. System displays edit form pre-populated with current book data
6. Staff member modifies desired fields (title, author, publisher, description, publication date, status)
7. Staff member updates category selections as needed
8. Staff member optionally uploads new cover image file
9. Staff member submits the updated information
10. System validates all updated information for correctness
11. System checks book existence and permissions
12. System validates cover image format if new image provided
13. System updates book record with modified information
14. System updates category associations as specified
15. System uploads new cover image and updates reference if provided
16. System saves all changes and displays success confirmation

**Alternative Flows:**
- 11.1: Status Change Only
  - 6a. Staff member only modifies book status (Available, Unavailable, Under Maintenance)
  - 13a. System updates book status without affecting other fields
- 11.2: Category Reassignment
  - 7a. Staff member changes category selections
  - 14a. System updates category associations with new selections
- 11.3: Cover Image Replacement
  - 8a. Staff member uploads new cover image
  - 15a. System validates new image format and replaces existing image
- 11.4: Cover Image Removal
  - 8a. Staff member chooses to remove existing image
  - 15a. System removes image reference and deletes file

**Exceptions:**
- 11.0.E1: Book Not Found
  - 4a. System cannot locate book with specified identifier
  - 4b. System displays "Book not found" error message
  - 4c. System redirects to book listing page
- 11.0.E2: Invalid Data Update
  - 10a. System validation detects invalid data format or constraint violation
  - 10b. System highlights problematic fields with specific error messages
  - 10c. Staff member must correct invalid data before proceeding
- 11.0.E3: Image Upload Error
  - 12a. New cover image upload fails
  - 12b. System preserves existing image and displays error message
  - 12c. Staff member can retry upload or proceed without image change
- 11.0.E4: Category Loading Error
  - 5a. System fails to load available categories
  - 5b. System displays simplified edit form without category options
  - 5c. Staff member can proceed with other modifications
- 11.0.E5: Concurrent Edit Conflict
  - 16a. Another user modifies the book simultaneously
  - 16b. System detects conflict and displays warning message
  - 16c. System requests refresh and review of changes

**Priority:** High

**Frequency of Use:** 100-300 times per week

**Business Rules:** BR-06, BR-22

**Other Information:**
- Book status can be changed to: Available, Unavailable, Under Maintenance
- Category changes immediately affect book browsing and search results
- Cover images are stored securely with proper file management
- System maintains complete book modification history
- Book copies are not affected by book information updates
- All changes are saved atomically to ensure data consistency

**Assumptions:**
- Staff have authority to make requested changes within their role permissions
- Book information updates don't violate data integrity constraints
- File upload functionality is operational for cover image updates
- Category updates are immediately reflected in the system

---

#### UC012: Delete Book

**Use Case ID and Name:** UC012 - Delete Book

**Author and Date Created:** Technical Lead, June 25, 2025

**Primary Actor:** Librarian, Admin

**Secondary Actors:** System

**Trigger:** Staff member needs to remove a book from the catalog for business reasons

**Description:** This use case enables authorized library staff to permanently remove book titles from the catalog. The system ensures all dependencies are resolved (no active loans or reservations) and maintains data integrity throughout the deletion process.

**Preconditions:**
- PRE-1: Staff member must be logged into the system
- PRE-2: Staff member must have book deletion permissions
- PRE-3: Book must exist in the catalog
- PRE-4: Book must not have any active loans
- PRE-5: Book must not have any active reservations

**Postconditions:**
- POST-1: Book record is permanently removed from catalog
- POST-2: All associated book copies are deleted automatically
- POST-3: Book-category associations are automatically removed
- POST-4: Deletion is recorded for audit purposes
- POST-5: Historical loan records are preserved for audit purposes

**Normal Flow (12.0):**
1. Staff member accesses book management page
2. System displays book listing with search functionality
3. Staff member selects "Delete" option for the desired book
4. System retrieves book details including all associated copies
5. System performs comprehensive dependency check
6. System checks for active loans associated with the book
7. System checks for active reservations associated with the book
8. System displays book details with dependency check results
9. Staff member reviews dependency information and confirms intention to delete
10. System prompts for deletion confirmation
11. Staff member confirms deletion action
12. System removes book record from catalog
13. System automatically removes associated copies and category relationships
14. System displays deletion success confirmation and updates book listing

**Alternative Flows:**
- 12.1: Book Status Change (Alternative to Deletion)
  - 9a. Staff member chooses to change book status to Unavailable instead of deletion
  - 12a. System updates book status rather than deleting
  - 14a. Book remains in catalog but marked as unavailable

**Exceptions:**
- 12.0.E1: Active Dependencies Found
  - 6a. System detects active loans for the book
  - 6b. System displays error "Cannot delete book with active loans"
  - 6c. Staff member must wait for loans to be returned before deletion can proceed
- 12.0.E2: Active Reservations Found
  - 7a. System detects active reservations for the book
  - 7b. System displays error "Cannot delete book with active reservations"
  - 7c. Staff member must cancel reservations before deletion can proceed
- 12.0.E3: Book Not Found
  - 4a. System cannot locate book with specified identifier
  - 4b. System displays "Book not found" error message
  - 4c. System redirects to book listing page
- 12.0.E4: Insufficient Permissions
  - 1a. Staff member lacks deletion permissions
  - 1b. System displays "Unauthorized" error message
  - 1c. Staff member must request higher authorization or contact administrator
- 12.0.E5: Data Constraint Violation
  - 12a. System encounters constraint violation during deletion
  - 12b. System displays error message with details
  - 12c. System suggests checking dependencies and trying again

**Priority:** Medium

**Frequency of Use:** 10-50 times per month

**Business Rules:** BR-06, BR-07, BR-22

**Other Information:**
- Deletion is permanent and cannot be undone without data recovery
- Historical loan records are preserved even after book deletion
- System maintains deletion audit trail for accountability
- Book copies are automatically deleted when parent book is removed
- All data relationships are properly maintained during deletion process

**Assumptions:**
- All business dependencies are properly identified by the dependency checking logic
- Staff have authority to delete books within their permission level
- Backup and recovery procedures are in place for accidental deletions
- System properly manages related data during deletion operations

---

#### UC013: Search Books

**Use Case ID and Name:** UC013 - Search Books

**Author and Date Created:** Technical Lead, June 25, 2025

**Primary Actor:** Guest, Member, Librarian, Admin

**Secondary Actors:** System

**Trigger:** User needs to find specific books in the catalog using search criteria

**Description:** This use case provides comprehensive search functionality for users to locate books in the catalog. Users can search by title, author, or ISBN, and receive paginated results showing availability information and detailed book information.

**Preconditions:**
- PRE-1: System must be accessible (authenticated users have full access, guests have read-only access)
- PRE-2: Book catalog must contain searchable records
- PRE-3: Search functionality must be operational

**Postconditions:**
- POST-1: Search results are displayed with pagination support
- POST-2: Results show book details including availability and copy information
- POST-3: Search activity is recorded for analytics purposes
- POST-4: User can access detailed book information from results
- POST-5: Search results maintain consistent ordering by title

**Normal Flow (13.0):**
1. User accesses book search page
2. System displays book listing with search form
3. User enters search criteria (title, author, or ISBN)
4. User submits the search request
5. System validates search input is not empty
6. System executes search across title, author, and ISBN fields
7. System retrieves matching books with category and copy information
8. System applies pagination to manage large result sets (default: 10 items per page)
9. System sorts results alphabetically by book title
10. System displays paginated search results with book details and availability counts
11. User can navigate through result pages or click book titles for detailed information

**Alternative Flows:**
- 13.1: Empty Search (Browse All Books)
  - 3a. User submits empty search term or navigates directly to book listing
  - 6a. System displays all books in paginated format
  - 10a. System shows complete catalog sorted by title
- 13.2: Pagination Navigation
  - 11a. User clicks on page numbers in pagination controls
  - 6a. System maintains search term and executes same search with different page
  - 10a. System preserves search context across page navigation
- 13.3: View Book Details
  - 11a. User clicks on book title or "Details" link from search results
  - 11b. System navigates to detailed book information page
  - 11c. System displays comprehensive book information including availability
- 13.4: Guest User Limitations
  - 11a. Guest user attempts to access restricted functionality (reservations, loan history)
  - 11b. System displays login prompt or registration invitation
  - 11c. Guest can continue browsing or choose to register/login for full access

**Exceptions:**
- 13.0.E1: No Results Found
  - 6a. System finds no books matching the search criteria
  - 10a. System displays "No books found matching your search" message
  - 10b. System shows empty results with suggestion to try different search terms
- 13.0.E2: Search Error
  - 6a. System encounters error during search execution
  - 6b. System displays user-friendly error message
  - 6c. System suggests trying again or contacting support if error persists
- 13.0.E3: Invalid Page Number
  - 9a. User navigates to non-existent page number
  - 9b. System automatically redirects to page 1 with same search criteria
  - 9c. System maintains original search term in the redirect
- 13.0.E4: System Connection Error
  - 7a. System cannot access data during search execution
  - 7b. System displays connection error message
  - 7c. System suggests retrying or checking system status

**Priority:** High

**Frequency of Use:** 1000-5000 times per day

**Business Rules:** BR-24

**Other Information:**
- Search performs partial matching across title, author, and ISBN fields
- Results include calculated copy counts and availability information
- Search results are paginated with configurable page sizes
- Search executes across multiple fields simultaneously for comprehensive results
- Book categories are displayed to help users identify relevant books
- Search functionality integrates with main book browsing features
- Guest users can search and view books but cannot perform member actions (borrow, reserve)
- Guest access encourages library membership by showcasing available resources

**Assumptions:**
- System performance supports efficient search operations
- Users understand basic search concepts and expect partial matching behavior
- Search performance is acceptable for typical catalog size
- Pagination provides adequate navigation for result sets

---

#### UC014: Browse by Category

**Use Case ID and Name:** UC014 - Browse by Category

**Author and Date Created:** Technical Lead, June 25, 2025

**Primary Actor:** Guest, Member, Librarian, Admin

**Secondary Actors:** System

**Trigger:** User wants to explore books organized by specific categories

**Description:** This use case enables users to browse the book catalog by selecting specific categories, providing a filtered view of books within chosen subject areas. Users can discover books through organized category navigation.

**Preconditions:**
- PRE-1: System must be accessible (authenticated users have full access, guests have read-only access)
- PRE-2: Categories must be defined in the system
- PRE-3: Books must be properly assigned to categories
- PRE-4: Category management must be operational

**Postconditions:**
- POST-1: Category-filtered book list is displayed with pagination
- POST-2: Books show complete information including availability and copy counts
- POST-3: Category selection context is maintained throughout browsing activity
- POST-4: Browse activity is recorded for analytics purposes
- POST-5: Category-specific book count is visible in the interface

**Normal Flow (14.0):**
1. User accesses category browsing page via navigation menu
2. System retrieves and displays all available categories
3. User selects a specific category of interest from the category listing
4. System validates selected category exists
5. System retrieves all books assigned to the selected category
6. System includes category and availability information for each book
7. System displays category-filtered book list with pagination
8. System shows category name, description, and total book count for selected category
9. User can view individual book details or return to category selection
10. System maintains category filter context during pagination navigation
11. System records category browsing activity for analytics and recommendations

**Alternative Flows:**
- 14.1: Category Details View
  - 3a. User clicks on category name to view detailed category information
  - 5a. System retrieves category with complete book list and metadata
  - 7a. System displays category information along with associated books
- 14.2: Multiple Category Browsing
  - 9a. User navigates back to category list to explore other categories
  - 2a. System maintains browsing history for quick category switching
  - 11a. System tracks category browsing patterns for recommendations
- 14.3: Empty Category Handling
  - 5a. Selected category contains no books
  - 7a. System displays "No books found in this category" message
  - 8a. System suggests browsing other categories or returning to main catalog
- 14.4: Guest User Experience
  - 9a. Guest user browses categories and books successfully
  - 9b. Guest user attempts to perform member-only actions (reservations, check-outs)
  - 9c. System displays registration invitation with benefits of membership

**Exceptions:**
- 14.0.E1: Category Not Found
  - 4a. System cannot locate selected category identifier
  - 4b. System displays "Category not found" error message
  - 4c. System redirects to category listing page
- 14.0.E2: Empty Category Collection
  - 2a. System finds no categories defined in the system
  - 2b. System displays "No categories available" message
  - 2c. System suggests browsing all books or contacting administrator
- 14.0.E3: Category Loading Error
  - 2a. System encounters error during category loading
  - 2b. System displays fallback interface with error message
  - 2c. System allows direct book browsing without category filtering
- 14.0.E4: Books Loading Error
  - 5a. System fails to load books for selected category
  - 5b. System displays error message and suggests trying different category
  - 5c. System maintains category selection context for retry

**Priority:** High

**Frequency of Use:** 500-2000 times per day

**Business Rules:** BR-24

**Other Information:**
- Categories support relationships allowing books to belong to multiple categories
- Books can be associated with categories for better organization and discovery
- Category browsing uses efficient processing for good performance
- Category information includes name, description, and calculated book counts
- System tracks category popularity based on browsing frequency
- Pagination maintains category context for seamless navigation
- Category browsing integrates with main book search and listing functionality
- Guest users can browse categories and view books but cannot perform member actions
- Category browsing serves as a discovery tool to attract new library members

**Assumptions:**
- Categories are logically organized and provide meaningful book groupings
- Books are properly assigned to appropriate categories by library staff
- Category-based queries perform efficiently with proper system design
- Users find category browsing intuitive for book discovery and exploration

---


### Book Copy Management Use Cases

#### UC015: Add Copy

**Use Case ID and Name:** UC015 - Add Copy

**Author and Date Created:** Technical Lead, June 25, 2025

**Primary Actor:** Librarian, Admin

**Secondary Actors:** System

**Trigger:** Library staff needs to add additional physical copies of an existing book to the inventory

**Description:** This use case allows authorized library staff to create new physical copy records for books already in the catalog. Staff can add individual copies with unique tracking numbers to maintain proper inventory control and availability tracking.

**Preconditions:**
- PRE-1: Actor must be authenticated in the system
- PRE-2: Actor must have copy management permissions (Librarian or Admin role)
- PRE-3: Book must exist in the catalog
- PRE-4: System must be accessible
- PRE-5: Copy management system must be operational

**Postconditions:**
- POST-1: New book copy record is created in inventory
- POST-2: Copy is assigned unique ID and auto-generated copy number
- POST-3: Copy status is set to Available
- POST-4: Book availability statistics are automatically recalculated
- POST-5: Copy addition is logged for audit purposes

**Normal Flow (15.0):**
1. Staff member accesses copy management page
2. System displays book copy creation form
3. Staff member searches for and selects the target book
4. System validates book exists in catalog
5. Staff member optionally provides custom copy number or lets system auto-generate
6. System validates copy number uniqueness within the book
7. Staff member submits the copy creation request
8. System generates automatic copy number if not provided (format: ISBN-XXX)
9. System creates new copy record with Available status
10. System saves copy information and updates book availability statistics
11. System displays success confirmation with copy details

**Alternative Flows:**
- 15.1: Auto-Generated Copy Number
  - 5a. Staff member leaves copy number field empty
  - 8a. System counts existing copies and generates next sequential number
- 15.2: Custom Copy Number
  - 5a. Staff member provides specific copy number
  - 6a. System validates uniqueness within the same book
- 15.3: Multiple Copy Creation
  - 5a. Staff member specifies quantity to create multiple copies at once
  - 9a. System creates multiple copies with sequential numbering

**Exceptions:**
- 15.0.E1: Book Not Found
  - 4a. System cannot locate selected book
  - 4c. Staff member must verify book exists in catalog before proceeding
- 15.0.E2: Duplicate Copy Number
  - 6a. System detects copy number already exists for the book
  - 6c. Staff member must provide different copy number or use auto-generation
- 15.0.E3: Invalid Copy Data
  - 7a. System validation detects missing or invalid information
  - 7c. Staff member must correct invalid data before resubmitting
- 15.0.E4: System Error
  - 10a. System encounters error during copy creation
  - 10c. Staff member should retry operation or contact administrator

**Priority:** High

**Frequency of Use:** 100-500 times per week

**Business Rules:** BR-06, BR-09, BR-22

**Other Information:**
- Copy numbers follow format: ISBN-XXX (e.g., "978-123-456-001")
- Default status is Available for new copies
- Copy uniqueness is enforced within each book
- System maintains proper relationships between books and their copies
- Initial copies are typically created during book acquisition

**Assumptions:**
- Staff understand the relationship between books and their individual copies
- Copy numbers are primarily for internal tracking and inventory management
- System maintains data integrity between books and copies
- Copy creation typically occurs shortly after book acquisition

---

#### UC016: Update Copy Status

**Use Case ID and Name:** UC016 - Update Copy Status

**Author and Date Created:** Technical Lead, June 25, 2025

**Primary Actor:** Librarian, Admin

**Secondary Actors:** System

**Trigger:** Staff member needs to change a book copy's status due to condition changes, maintenance, or operational requirements

**Description:** This use case allows authorized library staff to update the status of individual book copies to reflect their current condition and availability. Status updates ensure accurate inventory tracking and proper lending operations.

**Preconditions:**
- PRE-1: Staff member must be logged into the system
- PRE-2: Staff member must have copy management permissions
- PRE-3: Book copy must exist in the inventory
- PRE-4: Staff member must have authorization for the status change

**Postconditions:**
- POST-1: Copy status is updated to reflect new condition
- POST-2: Book availability statistics are automatically updated
- POST-3: Status change is recorded with timestamp for audit purposes
- POST-4: System validates that status change is appropriate
- POST-5: Other system components reflect the updated status

**Normal Flow (16.0):**
1. Staff member accesses copy management page
2. System provides copy search functionality
3. Staff member searches for target copy by copy number or book information
4. System displays current copy information including current status
5. Staff member selects new status from available options
6. System validates the status change request
7. System checks for any conflicts with active loans or reservations
8. Staff member confirms the status change
9. System updates the copy status
10. System saves the changes
11. System displays successful status update confirmation

**Alternative Flows:**
- 16.1: Bulk Status Update
  - 3a. Staff member selects multiple copies of the same book
  - 5a. Staff member applies same status change to all selected copies
  - 9a. System processes each copy individually
- 16.2: Status Update with Notes
  - 5a. Staff member provides additional notes about the status change
  - 6a. System validates both status and notes information
  - 9a. System updates status and stores associated notes
- 16.3: Automatic Status Updates
  - Note: Some status changes occur automatically (e.g., when books are borrowed or returned)
  - This use case covers manual status updates by library staff

**Exceptions:**
- 16.0.E1: Copy Not Found
  - 6a. System cannot locate copy with provided information
  - 6c. Staff member must verify copy exists and provide valid identification
- 16.0.E2: Active Loan Conflict
  - 7a. System detects copy has active loan when trying to mark as available
  - 7c. Staff member must process loan return before changing status
- 16.0.E3: Invalid Status Change
  - 7a. System detects status change is not appropriate (e.g., marking reserved copy as borrowed)
  - 7c. Staff member must choose appropriate status or resolve conflicts first
- 16.0.E4: Authorization Error
  - 6a. Staff member lacks permission for the requested status change
  - 6c. Staff member must contact supervisor or use appropriate account
- 16.0.E5: System Error
  - 9a. System encounters error during status update
  - 9c. Staff member should retry operation or contact administrator

**Priority:** High

**Frequency of Use:** 200-800 times per week

**Business Rules:** BR-06, BR-09, BR-10, BR-22

**Other Information:**
- Available status options include: Available, Borrowed, Reserved, Damaged, Lost
- Status changes affect book availability for borrowing and reservations
- Certain status transitions require additional validation (e.g., loan conflicts)
- Status changes are immediately reflected in availability searches
- Historical status changes are maintained for audit purposes
- System prevents invalid status transitions that would violate business rules

**Assumptions:**
- Status changes accurately reflect the physical condition and availability of copies
- Staff understand the implications of different status values
- System maintains proper validation to prevent data inconsistencies
- Status updates are typically made promptly when copy conditions change

---

#### UC017: Remove Copy

**Use Case ID and Name:** UC017 - Remove Copy

**Author and Date Created:** Technical Lead, June 25, 2025

**Primary Actor:** Librarian, Admin

**Secondary Actors:** System

**Trigger:** Staff member needs to permanently remove a book copy from inventory due to loss, irreparable damage, or disposal

**Description:** This use case allows authorized library staff to permanently remove individual book copies from the system inventory. The system ensures proper validation to prevent removal of copies with active obligations and maintains data integrity.

**Preconditions:**
- PRE-1: Staff member must be logged into the system
- PRE-2: Staff member must have copy deletion permissions
- PRE-3: Book copy must exist in the inventory
- PRE-4: Copy must not have any active loans
- PRE-5: Copy must not have any active reservations

**Postconditions:**
- POST-1: Book copy record is permanently removed from inventory
- POST-2: Book availability statistics are automatically updated
- POST-3: Copy removal is recorded for audit purposes
- POST-4: Historical loan records are preserved for audit trail
- POST-5: System data integrity is maintained

**Normal Flow (17.0):**
1. Staff member accesses copy management page
2. System provides copy search functionality
3. Staff member searches for and selects target copy for removal
4. System displays copy details and checks for any dependencies
5. System validates that copy has no active loans or reservations
6. System displays dependency check results and removal confirmation dialog
7. Staff member reviews information and confirms removal intention
8. System removes the book copy record from inventory
9. System saves the changes
10. System displays removal confirmation message
11. Book availability counts are automatically updated

**Alternative Flows:**
- 17.1: Copy Removal During Book Deletion
  - Note: When an entire book is deleted, all its copies are automatically removed
  - This use case covers individual copy removal while keeping the book record
- 17.2: Mark as Lost Instead of Removal
  - 7a. Staff member chooses to mark copy as "Lost" status instead of deletion
  - 8a. System updates copy status rather than removing the record
  - 9a. Copy remains in system but is marked as unavailable
- 17.3: Bulk Copy Removal
  - 3a. Staff member identifies multiple copies from same book for removal
  - 5a. System performs dependency check for each copy individually
  - 8a. System processes each copy removal separately

**Exceptions:**
- 17.0.E1: Active Loan Found
  - 5a. System detects copy has active loan
  - 5c. Staff member must process loan return before copy removal
- 17.0.E2: Active Reservation Found
  - 5a. System detects copy has active reservations
  - 5c. Staff member must cancel or reassign reservations before removal
- 17.0.E3: Copy Not Found
  - 5a. System cannot locate copy with provided information
  - 5c. Staff member must verify copy exists and provide valid identification
- 17.0.E4: Insufficient Permissions
  - 8a. Staff member lacks permission to delete copies
  - 8c. Staff member must contact supervisor or use appropriate account
- 17.0.E5: System Error
  - 8a. System encounters error during copy removal
  - 8c. Staff member should retry operation or contact administrator

**Priority:** Medium

**Frequency of Use:** 10-50 times per week

**Business Rules:** BR-06, BR-08, BR-22

**Other Information:**
- Copy removal is permanent and cannot be undone
- Historical loan and reservation records are preserved even after copy removal
- System prevents removal of copies with outstanding obligations
- Removal requires explicit confirmation to prevent accidental deletions
- Alternative to deletion is marking copies as "Lost" or "Damaged" status
- Copy removal immediately affects book availability counts and statistics

**Assumptions:**
- Staff understand that copy removal is permanent and irreversible
- Proper procedures are followed before marking copies for removal
- System maintains adequate audit trails for removed copies
- Copy removal decisions are made based on legitimate business needs

---


### Loan Management Use Cases

#### UC018: Check Out

**Use Case ID and Name:** UC018 - Check Out

**Author and Date Created:** Technical Lead, June 25, 2025

**Primary Actor:** Librarian, Admin

**Secondary Actors:** System

**Trigger:** Member requests to borrow a book or staff member processes a checkout

**Description:** This use case enables authorized library staff to loan book copies to eligible members. The system creates loan records, updates book availability, and establishes due dates according to library policies (typically 14 days).

**Preconditions:**
- PRE-1: Staff member must be logged into the system
- PRE-2: Staff member must have loan management permissions
- PRE-3: Member must have active membership status
- PRE-4: Book copy must be available for borrowing
- PRE-5: Member must be eligible to borrow (no excessive fines or restrictions)

**Postconditions:**
- POST-1: Loan record is created with active status
- POST-2: Book copy status is updated to borrowed
- POST-3: Due date is established based on library policy
- POST-4: Member's active loan count is updated
- POST-5: Checkout transaction is recorded for audit purposes
- POST-6: Member receives loan confirmation if notifications are enabled

**Normal Flow (18.0):**
1. Staff member accesses loan management interface
2. System provides member and book copy search functionality
3. Staff member identifies and validates member eligibility
4. System retrieves member information and confirms active membership
5. Staff member identifies available book copy for checkout
6. System validates book copy availability
7. Staff member reviews loan details and default due date
8. System performs final validation checks
9. Staff member confirms checkout transaction
10. System creates loan record with active status
11. System updates book copy status to borrowed
12. System saves all changes and generates confirmation
13. System displays successful checkout confirmation with loan details

**Alternative Flows:**
- 18.1: Custom Due Date
  - 7a. Staff member specifies custom due date based on special circumstances
  - 8a. System validates custom due date is after current date
- 18.2: Multiple Book Checkout
  - 5a. Staff member processes multiple book copies for same member
  - 11a. System updates multiple copy statuses in single transaction
- 18.3: Member Eligibility Override
  - 4a. System detects member eligibility issues but staff has override authority
  - 8a. System logs override reason and processes loan with special handling

**Exceptions:**
- 18.0.E1: Member Not Found
  - 4a. System cannot locate member with provided information
  - 4c. Staff member must verify member information or register new member
- 18.0.E2: Member Not Active
  - 4a. System detects member is not in active status
  - 4c. Staff member must resolve member status before checkout
- 18.0.E3: Copy Not Available
  - 6a. System detects book copy is not available for borrowing
  - 6c. Staff member can suggest alternative copies or place reservation
- 18.0.E4: Invalid Due Date
  - 8a. System detects invalid due date (past date or too far in future)
  - 8c. Staff member must provide valid due date within policy limits
- 18.0.E5: System Error
  - 10a. System encounters error during loan creation
  - 10c. Staff member should retry operation or contact administrator

**Priority:** High

**Frequency of Use:** 500-2000 times per day

**Business Rules:** BR-13, BR-14, BR-16, BR-22

**Other Information:**
- Standard loan period is 14 days for most materials
- Loan status options include: Active, Returned, Overdue, Lost
- Copy status changes from Available to Borrowed during checkout
- System maintains transaction integrity between loan creation and copy status updates
- Member eligibility validation prevents lending to restricted accounts
- Checkout confirmations can be printed or emailed to members

**Assumptions:**
- Member information is current and accurate in the system
- Library loan policies are consistent and well-defined
- Staff have proper training on loan management procedures
- System maintains data consistency between loans and copy statuses

---

#### UC019: Return Book

**Use Case ID and Name:** UC019 - Return Book

**Author and Date Created:** Technical Lead, June 25, 2025

**Primary Actor:** Librarian, Admin

**Secondary Actors:** System

**Trigger:** Member returns a borrowed book or staff member processes returned books

**Description:** This use case enables library staff to process book returns by updating loan records, calculating any applicable fines for overdue returns, and making books available for other members to borrow.

**Preconditions:**
- PRE-1: Staff member must be logged into the system
- PRE-2: Staff member must have loan management permissions
- PRE-3: Loan must exist with active or overdue status
- PRE-4: Physical book must be available for inspection and return processing

**Postconditions:**
- POST-1: Loan record is updated with return date and returned status
- POST-2: Book copy status is updated to available
- POST-3: Member's active loan count is automatically updated
- POST-4: Overdue fines are calculated and applied if applicable
- POST-5: Return transaction is recorded for audit purposes
- POST-6: Book becomes available for other members immediately
- POST-7: Member's outstanding fines balance is updated if applicable

**Normal Flow (19.0):**
1. Staff member accesses return processing interface
2. System provides book return functionality with loan lookup capability
3. Staff member scans returned book or enters copy identifier to find active loan
4. System identifies active loan and displays loan details
5. System shows loan information including member, book, and due date details
6. Staff member inspects physical book condition for any damage
7. Staff member confirms book condition and processes return
8. System records current date as return date
9. System calculates overdue fines if book is returned after due date
10. System updates loan status to returned
11. System updates book copy status to available
12. System creates fine record if overdue and updates member's outstanding balance
13. System saves all changes and generates return confirmation
14. System displays successful return confirmation with any fine details

**Alternative Flows:**
- 19.1: Overdue Return with Fine
  - 9a. System calculates days overdue and applies standard fine rate
  - 12a. System creates fine record and adds to member's outstanding balance
  - 14a. System displays return confirmation with fine amount details
- 19.2: Damaged Book Return
  - 6a. Staff member identifies book damage during inspection
  - 11a. Staff member updates copy status to damaged instead of available
  - 12a. System applies damage fine according to library replacement cost policy
- 19.3: Multiple Book Returns
  - 3a. Staff member processes multiple returns for same member
  - 14a. System handles each return independently with separate confirmations

**Exceptions:**
- 19.0.E1: Loan Not Found
  - 4a. System cannot locate active loan for provided book copy
  - 4c. Staff member should verify loan information or check loan status
- 19.0.E2: Loan Already Returned
  - 8a. System detects loan is already in returned status
  - 8c. Staff member should verify loan status or escalate to supervisor
- 19.0.E3: System Error During Processing
  - 13a. System encounters error during return processing
  - 13c. Staff member should retry operation or contact administrator
- 19.0.E4: Fine Calculation Error
  - 9a. System encounters error calculating overdue fines
  - 9c. Staff member can complete return and handle fine calculation separately

**Priority:** High

**Frequency of Use:** 500-2000 times per day

**Business Rules:** BR-10, BR-15, BR-22

**Other Information:**
- Fine calculation typically uses standard rate per day for overdue books
- Fine amounts are reasonable and capped to prevent excessive charges
- New fines require member attention and may affect future borrowing privileges
- System maintains complete return history for audit and reporting purposes
- Book condition assessment helps maintain library collection quality
- Return processing updates availability immediately for other borrowing requests

**Assumptions:**
- Staff can accurately assess book condition during return inspection
- Fine calculation rules are consistently applied across all returns
- Member contact information is available for overdue notifications
- Physical security prevents unauthorized returns or tampering
- Historical return data preserved for audit and reporting purposes

**Assumptions:**
- Staff can accurately assess book condition during return inspection
- Fine calculation rules are consistently applied across all returns
- Member contact information is available for overdue notifications
- Physical security prevents unauthorized returns or tampering

---

#### UC020: Renew Loan

**Use Case ID and Name:** UC020 - Renew Loan

**Author and Date Created:** Technical Lead, June 25, 2025

**Primary Actor:** Member, Librarian, Admin

**Secondary Actors:** System

**Trigger:** Member requests loan extension before due date or staff member processes extension request

**Description:** This use case allows members and staff to extend loan periods for currently borrowed books, subject to library policies and member eligibility requirements. Extensions enable continued access to library materials without incurring overdue fines.

**Preconditions:**
- PRE-1: User must be logged into the system
- PRE-2: Loan must exist with active status
- PRE-3: Member must have active membership standing
- PRE-4: New due date must be valid and within extension limits
- PRE-5: No active reservations exist for the book (library policy)

**Postconditions:**
- POST-1: Loan due date is updated to the new extended date
- POST-2: Extension transaction is recorded for audit purposes
- POST-3: Member receives extension confirmation if notifications are enabled
- POST-4: New due date is saved in the loan record

**Normal Flow (20.0):**
1. User accesses loan extension interface
2. System displays current active loans for member or provides member search for staff
3. User selects specific active loan to extend
4. System validates loan exists and has active status
5. User specifies new due date for the loan extension
6. System validates new due date is after current due date and within maximum extension period
7. System verifies member has active membership status
8. System displays extension details showing old and new due dates for confirmation
9. User confirms extension request
10. System updates loan due date with new extension date
11. System saves changes and processes extension
12. System displays successful extension confirmation with new due date

**Alternative Flows:**
- 20.1: Maximum Extension Period Applied
  - 6a. System enforces maximum extension date (typically 30 days from current date)
  - 6b. System prevents extensions beyond library policy limits
- 20.2: Staff-Assisted Extension
  - 2a. Staff member looks up member loans for processing
  - 3a. Staff processes extension on behalf of member
  - 12a. System includes staff processing information in transaction record
- 20.3: Extension with Policy Override
  - 6a. System detects potential policy violations but allows conditional extension
  - 9a. Staff provides override reason for policy exception

**Exceptions:**
- 20.0.E1: Loan Not Found
  - 4a. System cannot locate loan with provided information
  - 4c. User must verify loan exists and provide correct loan identification
- 20.0.E2: Loan Not Active
  - 4a. System detects loan is not in active status
  - 4c. Extensions are only allowed for active loans
- 20.0.E3: Invalid Extension Date
  - 6a. System detects new due date is not after current due date
  - 6c. User must provide valid future date for extension
- 20.0.E4: Extension Limit Exceeded
  - 6a. System detects new due date exceeds maximum extension period
  - 6c. User must choose date within allowed extension timeframe
- 20.0.E5: Member Not Active
  - 7a. System detects member does not have active membership status
  - 7c. Member status must be resolved before extension is allowed

**Priority:** High

**Frequency of Use:** 200-800 times per day

**Business Rules:** BR-14, BR-16, BR-22

**Other Information:**
- Maximum extension period is typically 30 days from current date
- Extension validation prevents extending loans beyond reasonable timeframes
- Active membership status is required for all loan extensions
- System maintains transaction integrity during extension processing
- Extensions help prevent overdue fines when members need additional time
- Staff can process extensions on behalf of members when necessary

**Assumptions:**
- Extension policies are reasonable and meet member needs
- System accurately validates date ranges and member eligibility
- Extension notifications reach members reliably when enabled
- Staff understand extension policies and override procedures
- Extension policies are enforced at system level for consistency

**Assumptions:**
- Extension policies are reasonable and meet member needs
- System accurately validates date ranges and member eligibility
- Extension notifications reach members reliably
- Staff understand extension policies and override procedures

---

#### UC021: View Loan History

**Use Case ID and Name:** UC021 - View Loan History

**Author and Date Created:** Technical Lead, June 25, 2025

**Primary Actor:** Member, Librarian, Admin

**Secondary Actors:** System

**Trigger:** User needs to access current and historical loan information

**Description:** This use case provides comprehensive access to loan records, enabling members to track their borrowing history and staff to review loan patterns, resolve disputes, and provide member assistance. Users can view both current and past loans with detailed information.

**Preconditions:**
- PRE-1: User must be logged into the system
- PRE-2: Members can only view their own loan history
- PRE-3: Staff need appropriate permissions to view member loan history
- PRE-4: Loan records must exist in the system

**Postconditions:**
- POST-1: Loan history is displayed according to user permissions with pagination
- POST-2: Current active loans are highlighted with real-time due date status
- POST-3: Historical loans show complete information including return dates
- POST-4: Access to loan history is recorded for audit purposes

**Normal Flow (21.0):**
1. User accesses loan history interface
2. System displays member selection for staff or auto-loads data for member self-service
3. User selects search criteria including date range or uses pagination controls
4. System retrieves loan records with complete book and member information
5. System organizes loans by status (Active, Returned, Overdue, Lost)
6. System displays paginated loan list with book titles, copy numbers, dates, and status
7. User can sort loans by loan date, due date, or status
8. User selects specific loan for detailed information
9. System displays comprehensive loan details including all related information
10. User can navigate between different loans or access export functionality
11. System records history access for audit purposes

**Alternative Flows:**
- 21.1: Current Loans Only View
  - 4a. User applies filter to show only active loans
  - 5a. System retrieves only current active loans with real-time overdue status
  - 6a. System highlights overdue loans for immediate attention
- 21.2: Overdue Loans Report
  - 4a. User requests overdue loans report
  - 5a. System retrieves all overdue loans with calculated fine amounts
  - 6a. System displays overdue information with fine calculations
- 21.3: Search Within History
  - 3a. User enters search criteria for book title, author, or other details
  - 4a. System searches across loan records for matching criteria
  - 6a. System displays matching loan records with highlighted search terms
- 21.4: Export History Data
  - 10a. User selects export option for record keeping
  - 10b. System generates comprehensive report from loan data
  - 10c. System provides download functionality for exported information

**Exceptions:**
- 21.0.E1: No Loan History Found
  - 5a. System finds no loan records matching specified criteria
  - 5c. User can adjust search criteria, date range, or pagination settings
- 21.0.E2: Access Denied
  - 2a. System detects insufficient permissions for requested member data
  - 2c. User must request appropriate permissions or contact administrator
- 21.0.E3: Data Retrieval Error
  - 4a. System encounters error during loan history retrieval
  - 4c. User can retry operation or contact technical support
- 21.0.E4: Large Dataset Management
  - 4a. System detects request would return excessive records
  - 4c. User must specify more restrictive search criteria or use pagination

**Priority:** Medium

**Frequency of Use:** 100-500 times per day

**Business Rules:** BR-03, BR-22, BR-24

**Other Information:**
- Loan history includes all loan statuses for complete tracking
- Search functionality works across member names, book titles, and copy numbers
- Pagination is implemented to handle large datasets efficiently
- System automatically loads related member and book information
- Overdue status is calculated in real-time based on current date
- Historical data is preserved indefinitely for audit and compliance purposes
- Export functionality allows users to maintain personal or administrative records

**Assumptions:**
- Users understand the difference between current and historical loans
- Search functionality meets user expectations for finding specific loans
- System performance is adequate for typical loan history sizes
- Export functionality provides data in useful formats for users
- System maintains complete and accurate loan records over time
- Privacy policies are properly enforced for member loan data access
- Historical data remains accessible despite system updates or migrations
- Data processing performance is acceptable for typical loan history sizes

---


### Reservation Management Use Cases

#### UC022: Reserve Book

**Use Case ID and Name:** UC022 - Reserve Book

**Author and Date Created:** Technical Lead, June 25, 2025

**Primary Actor:** Member, Librarian, Admin

**Secondary Actors:** System

**Trigger:** A member wants to borrow a book that is currently unavailable because all copies are on loan

**Description:** This use case allows library members and staff to place reservations on books when no copies are available for immediate borrowing. The system places the member in a reservation queue and validates their eligibility to make reservations.

**Preconditions:**
- PRE-1: User must be authenticated in the system
- PRE-2: Member must have active membership status
- PRE-3: Book must exist in the library catalog
- PRE-4: All copies of the book must be currently on loan
- PRE-5: Member must not have an existing active reservation for the same book

**Postconditions:**
- POST-1: Reservation record is created with pending status
- POST-2: Member is placed in the reservation queue for the book
- POST-3: System records reservation date and member's queue position
- POST-4: Member receives confirmation of reservation placement
- POST-5: Reservation activity is logged for audit purposes

**Normal Flow (22.0):**
1. Member accesses the library catalog or staff assists member
2. System displays book search functionality
3. Member searches for and selects desired book
4. System displays book details showing "All copies on loan"
5. Member requests to place a reservation
6. System validates member's active membership status
7. System checks that book exists and all copies are borrowed
8. System verifies member has no existing reservation for this book
9. System confirms reservation details with member
10. Member confirms the reservation request
11. System creates reservation record and assigns queue position
12. System displays confirmation with estimated availability date

**Alternative Flows:**
- 22.1: Staff-Assisted Reservation
  - 1a. Staff member assists member with reservation process
  - 12a. Staff provides printed confirmation to member
- 22.2: Book Becomes Available
  - 7a. System detects a copy became available during the process
  - 7b. System offers immediate checkout instead of reservation
- 22.3: Member Places Multiple Reservations
  - 3a. Member selects multiple books for reservation
  - 11a. System processes each reservation individually

**Exceptions:**
- 22.0.E1: Member Not Found
  - 6a. System cannot locate member information
  - 6b. System displays error message
  - 6c. Process returns to member identification step
- 22.0.E2: Inactive Membership
  - 6a. System detects member's inactive status
  - 6b. System displays membership status message
  - 6c. Member must renew membership before placing reservation
- 22.0.E3: Book Not Found
  - 7a. System cannot locate the requested book
  - 7b. System displays book not found message
  - 7c. Process returns to book selection step
- 22.0.E4: Duplicate Reservation
  - 8a. System finds existing active reservation for same book
  - 8b. System displays duplicate reservation message
  - 8c. Member is shown their existing reservation details
- 22.0.E5: Available Copy Found
  - 7a. System detects available copies during validation
  - 7b. System redirects to checkout process instead

**Priority:** High

**Frequency of Use:** 200-800 times per week

**Business Rules:** BR-17, BR-18, BR-22

**Other Information:**
- Queue position is determined by reservation date (first-come, first-served)
- Members can have multiple active reservations for different books
- System automatically notifies members when reserved books become available
- Reservations expire if not fulfilled within 30 days
- Staff can override certain validation rules with proper authorization

**Assumptions:**
- Members understand the reservation queue system
- Book availability status is updated in real-time
- Member contact information is current for notifications
- System maintains accurate inventory tracking

---

#### UC023: Cancel Reservation

**Use Case ID and Name:** UC023 - Cancel Reservation

**Author and Date Created:** Technical Lead, June 25, 2025

**Primary Actor:** Member, Librarian, Admin

**Secondary Actors:** System

**Trigger:** A member or staff member needs to remove an existing active reservation

**Description:** This use case enables library members and staff to cancel pending book reservations. When cancelled, the member is removed from the reservation queue and other members move up in position.

**Preconditions:**
- PRE-1: User must be authenticated in the system
- PRE-2: Active reservation must exist in the system
- PRE-3: Reservation must be in pending status (not fulfilled or expired)
- PRE-4: Member can only cancel their own reservations (unless staff override)

**Postconditions:**
- POST-1: Reservation status is updated to cancelled
- POST-2: Member is removed from the reservation queue
- POST-3: Other members' queue positions are automatically adjusted
- POST-4: Cancellation confirmation is provided to the user
- POST-5: Cancellation activity is logged for audit purposes

**Normal Flow (23.0):**
1. Member accesses their reservation list or staff searches for reservation
2. System displays active reservations with cancellation options
3. Member selects the reservation to cancel
4. System displays reservation details for confirmation
5. System prompts for cancellation confirmation
6. Member confirms the cancellation request
7. System validates reservation can be cancelled
8. System updates reservation status to cancelled
9. System adjusts queue positions for remaining members
10. System displays cancellation confirmation message

**Alternative Flows:**
- 23.1: Staff-Assisted Cancellation
  - 1a. Staff member looks up member's reservations
  - 6a. Staff confirms cancellation on behalf of member
  - 10a. System logs staff member's action
- 23.2: Emergency Cancellation
  - 7a. Staff overrides normal cancellation rules
  - 7b. System requires staff authorization for override
  - 10a. System logs emergency cancellation reason

**Exceptions:**
- 23.0.E1: Reservation Not Found
  - 3a. System cannot locate the specified reservation
  - 3b. System displays reservation not found message
  - 3c. User returns to reservation list
- 23.0.E2: Unauthorized Cancellation
  - 7a. Member attempts to cancel another member's reservation
  - 7b. System displays authorization error message
  - 7c. Cancellation process is terminated
- 23.0.E3: Invalid Reservation Status
  - 7a. Reservation is already fulfilled, cancelled, or expired
  - 7b. System displays inappropriate status message
  - 7c. User is returned to reservation details

**Priority:** High

**Frequency of Use:** 100-400 times per week

**Business Rules:** BR-24, BR-25

**Other Information:**
- Only pending reservations can be cancelled
- Cancelled reservations are retained for historical records
- Queue positions automatically recalculate when reservations are cancelled
- Members receive confirmation when cancellation is completed
- Staff can cancel reservations with appropriate authorization

**Assumptions:**
- Members understand the consequences of cancelling reservations
- System maintains accurate queue position information
- Cancellation processing is completed immediately

---

#### UC024: Fulfill Reservation

**Use Case ID and Name:** UC024 - Fulfill Reservation

**Author and Date Created:** Technical Lead, June 25, 2025

**Primary Actor:** Librarian, Admin

**Secondary Actors:** System, Member

**Trigger:** A reserved book becomes available for pickup and the next member in queue needs to be notified

**Description:** This use case enables library staff to fulfill pending reservations when books become available. The system notifies the next member in the queue and prepares the book for pickup within the specified timeframe.

**Preconditions:**
- PRE-1: Staff member must be authenticated with appropriate permissions
- PRE-2: Active reservation must exist in the system
- PRE-3: Book copy must be available for the reserved book
- PRE-4: Member must have current contact information for notifications

**Postconditions:**
- POST-1: Reservation status is updated to fulfilled
- POST-2: Book copy is held specifically for the member
- POST-3: Member is notified of book availability
- POST-4: Pickup deadline is established (typically 72 hours)
- POST-5: Fulfillment activity is logged for audit purposes

**Normal Flow (24.0):**
1. Staff member accesses the reservation queue management system
2. System displays pending reservations ordered by date and priority
3. Staff member identifies a book that has become available
4. System shows the next member in the reservation queue for that book
5. Staff member selects the reservation to fulfill
6. System validates the book copy is available
7. Staff member confirms fulfillment for the specific member
8. System updates reservation status to fulfilled
9. System reserves the specific copy for the member
10. System sends notification to member about availability
11. System establishes pickup deadline
12. System displays fulfillment confirmation with pickup details

**Alternative Flows:**
- 24.1: Automatic Fulfillment
  - 1a. System automatically detects returned book with pending reservations
  - 4a. System identifies next member in queue automatically
  - 11a. System proceeds with notification process
- 24.2: Multiple Copies Available
  - 6a. System displays multiple available copies for selection
  - 7a. Staff member chooses specific copy for the reservation
  - 9a. System reserves the selected copy
- 24.3: Member Contact Issues
  - 10a. System cannot reach member with provided contact information
  - 10b. Staff member updates member contact details
  - 10c. System retries notification process

**Exceptions:**
- 24.0.E1: No Active Reservations
  - 4a. System finds no pending reservations for the book
  - 4b. System displays no reservations message
  - 4c. Staff member returns to main reservation interface
- 24.0.E2: Copy Not Available
  - 6a. System detects no copies are available for reservation
  - 6b. System displays unavailable message
  - 6c. Fulfillment process is cancelled
- 24.0.E3: Member Contact Missing
  - 10a. System finds incomplete member contact information
  - 10b. System prompts staff to update member details
  - 10c. Fulfillment is paused until contact information is updated
- 24.0.E4: Notification Failure
  - 10a. System fails to send notification to member
  - 10b. System logs notification failure
  - 10c. Staff member must contact member manually

**Priority:** High

**Frequency of Use:** 200-600 times per week

**Business Rules:** BR-19, BR-21, BR-22

**Other Information:**
- Fulfilled reservations have 72-hour pickup window
- Copy status changes to reserved during fulfillment
- Queue processing follows first-come, first-served order
- Multiple notification methods may be used (email, phone, SMS)
- Unfulfilled reservations expire after pickup deadline

**Assumptions:**
- Staff understand the reservation queue priority system
- Members respond to pickup notifications within the timeframe
- System maintains accurate copy availability information
- Notification systems are reliable and functional

---

#### UC025: View Reservations

**Use Case ID and Name:** UC025 - View Reservations

**Author and Date Created:** Technical Lead, June 25, 2025

**Primary Actor:** Member, Librarian, Admin

**Secondary Actors:** System

**Trigger:** A user needs to view current or historical reservation information

**Description:** This use case provides access to reservation records, enabling members to track their reservation status and staff to manage reservation queues and assist members. Users can view, search, and filter reservations based on various criteria.

**Preconditions:**
- PRE-1: User must be authenticated in the system
- PRE-2: User must have appropriate permissions to view reservations
- PRE-3: Members can only view their own reservations
- PRE-4: Staff can view any member's reservations with proper authorization

**Postconditions:**
- POST-1: Reservation information is displayed according to user permissions
- POST-2: Current active reservations show status and queue position
- POST-3: Historical reservations show complete transaction history
- POST-4: User can navigate and search through reservation records
- POST-5: Access to reservation data is logged for audit purposes

**Normal Flow (25.0):**
1. User accesses the reservation management interface
2. System displays appropriate reservation view based on user role
3. User selects search criteria or filtering options
4. System retrieves matching reservation records
5. System organizes reservations by status and date
6. System displays reservation list with book titles, dates, and status
7. User can sort reservations by date, status, or queue position
8. User selects specific reservation for detailed information
9. System displays comprehensive reservation details
10. User can navigate between reservations or export information

**Alternative Flows:**
- 25.1: Active Reservations Only
  - 4a. User applies filter to show only active reservations
  - 6a. System displays queue position and estimated availability
  - 6b. System highlights reservations nearing expiration
- 25.2: Book-Specific Reservations
  - 3a. User searches for reservations for a specific book
  - 4a. System retrieves all reservations for that book
  - 6a. System displays complete reservation queue
- 25.3: Advanced Search
  - 3a. User enters search criteria for member name or book title
  - 4a. System searches across all reservation records
  - 6a. System displays matching results with highlighted terms
- 25.4: Export Reservations
  - 10a. User selects export option for record keeping
  - 10b. System generates downloadable report
  - 10c. User receives confirmation of export completion

**Exceptions:**
- 25.0.E1: No Reservations Found
  - 4a. System finds no reservations matching the criteria
  - 4b. System displays no results message
  - 4c. User can adjust search criteria or filters
- 25.0.E2: Access Denied
  - 2a. System detects insufficient permissions
  - 2b. System displays access denied message
  - 2c. User must contact administrator for proper access
- 25.0.E3: Data Retrieval Error
  - 4a. System encounters error during data retrieval
  - 4b. System displays user-friendly error message
  - 4c. User can retry operation or contact support
- 25.0.E4: Large Dataset Warning
  - 4a. System detects request would return excessive records
  - 4b. System suggests more specific search criteria
  - 4c. User can proceed with pagination or refine search

**Priority:** Medium

**Frequency of Use:** 100-500 times per day

**Business Rules:** BR-03, BR-22, BR-24

**Other Information:**
- Search functionality works across member names and book titles
- Queue position is calculated in real-time for active reservations
- Historical data is preserved for audit and compliance purposes
- Export functionality provides data in standard formats
- System maintains user privacy and data access controls

**Assumptions:**
- Users understand reservation status meanings and queue concepts
- System maintains complete and accurate reservation records
- Privacy policies are properly enforced for member data access
- Search and filtering functionality meets user performance expectations

---


### Fine Management Use Cases

#### UC026: Calculate Fine

**Use Case ID and Name:** UC026 - Calculate Fine

**Author and Date Created:** Technical Lead, June 25, 2025

**Primary Actor:** System, Librarian, Admin

**Secondary Actors:** Member

**Trigger:** A book is returned after its due date or staff needs to determine fine amounts for overdue materials

**Description:** This use case calculates overdue fines for late book returns based on the number of days overdue and the library's daily fine rate. The system automatically calculates and applies fines during the return process or allows staff to manually calculate fines when needed.

**Preconditions:**
- PRE-1: Valid loan record must exist in the system
- PRE-2: Loan must have a valid due date
- PRE-3: Daily fine rate is established ($0.50 per day)
- PRE-4: Current date can be determined accurately

**Postconditions:**
- POST-1: Fine amount is calculated based on days overdue
- POST-2: Fine record is created with pending status (for automatic creation)
- POST-3: Member's outstanding balance is updated
- POST-4: Fine is linked to the loan and member records
- POST-5: All changes are saved to the system

**Normal Flow (26.0):**
1. System or staff initiates fine calculation for a specific loan
2. System retrieves the loan record from the system
3. System validates that the loan exists and is in active status
4. System checks if the loan due date has passed
5. System calculates the number of days overdue
6. System applies the daily fine rate to determine total fine amount
7. System returns the calculated fine amount
**Alternative Flows:**
- 26.1: Automatic Fine Creation During Return
  - 1a. System detects late return during book return process
  - 5a. System creates fine record automatically
  - 7a. System adds fine to member's outstanding balance
- 26.2: No Fine Required
  - 4a. Loan is not overdue or does not exist
  - 7a. System returns zero fine amount
- 26.3: Manual Fine Creation
  - 1a. Staff manually creates fine for special circumstances
  - 6a. Staff specifies fine amount and reason
  - 7a. System creates fine record with staff override

**Exceptions:**
- 26.0.E1: Loan Not Found
  - 2a. System cannot locate loan with provided information
  - 2b. System displays loan not found message
  - 2c. Process returns to loan identification step
- 26.0.E2: Invalid Loan Status
  - 3a. Loan status is not active (already returned or lost)
  - 3b. System indicates no fine is applicable
  - 3c. System returns zero fine amount
- 26.0.E3: System Error
  - 6a. System encounters error during fine calculation
  - 6b. System displays error message
  - 6c. Staff member can retry operation or contact administrator

**Priority:** High

**Frequency of Use:** 20-100 calculations per day

**Business Rules:** BR-15, BR-22

**Other Information:**
- Standard daily fine rate is $0.50 for most materials
- Fine calculations are based on calendar days
- System automatically creates fines during return process for overdue items
- Staff can manually create fines for special circumstances
- All fine calculations are logged for audit purposes

**Assumptions:**
- System date and time are accurate and reliable
- Daily fine rate is appropriate for all book types
- Members are responsible for returning books on time
- Fine calculations are consistent across all loan types

---

#### UC027: Pay Fine

**Use Case ID and Name:** UC027 - Pay Fine

**Author and Date Created:** Technical Lead, June 25, 2025

**Primary Actor:** Librarian, Admin

**Secondary Actors:** Member, System

**Trigger:** A member makes a payment for outstanding library fines through staff assistance

**Description:** This use case enables library staff to process fine payments on behalf of members. When payment is received, the fine status is updated to paid and the member's outstanding balance is reduced accordingly.

**Preconditions:**
- PRE-1: Valid fine record must exist in the system
- PRE-2: Fine must be in pending status (not already paid or waived)
- PRE-3: Fine must be linked to a valid member account
- PRE-4: Staff member must have appropriate permissions to process payments

**Postconditions:**
- POST-1: Fine status is updated to paid
- POST-2: Member's outstanding balance is reduced by the fine amount
- POST-3: Payment transaction is recorded in the system
- POST-4: Payment confirmation is provided to staff and member
- POST-5: All changes are saved permanently to the system

**Normal Flow (27.0):**
1. Staff member accesses fine payment processing interface
2. System displays pending fines for the member
3. Staff member selects the fine to be paid
4. System validates that the fine exists and is in pending status
5. Staff member confirms payment details with member
6. System processes the payment and updates fine status to paid
7. System reduces member's outstanding balance by fine amount
8. System saves all changes to the system
9. System displays payment confirmation with receipt details

**Alternative Flows:**
- 27.1: Multiple Fine Payment
  - 3a. Staff selects multiple fines for the same member
  - 6a. System processes each fine payment individually
  - 9a. System provides consolidated payment confirmation
- 27.2: Partial Payment
  - 5a. Member provides partial payment toward fine
  - 6a. Staff creates new fine for remaining balance
  - 9a. System shows both payment and remaining balance

**Exceptions:**
- 27.0.E1: Fine Not Found
  - 4a. System cannot locate fine with provided information
  - 4b. System displays fine not found message
  - 4c. Staff member returns to fine selection step
- 27.0.E2: Fine Already Processed
  - 4a. Fine has already been paid or waived
  - 4b. System displays fine status message
  - 4c. Staff member selects different fine or confirms status
- 27.0.E3: Payment Processing Error
  - 8a. System encounters error during payment processing
  - 8b. System displays error message and rolls back changes
  - 8c. Staff member can retry payment or contact administrator

**Priority:** High

**Frequency of Use:** 50-200 times per week

**Business Rules:** BR-22

**Other Information:**
- Only pending fines can be paid through this process
- Payment confirmation includes receipt number and transaction details
- All payment transactions are logged for audit purposes
- Member's outstanding balance is updated immediately
- Staff can process multiple fine payments in sequence

**Assumptions:**
- Payment method validation is handled outside the system
- Member balance calculations are accurate and current
- Staff have proper training on payment processing procedures
- System maintains data consistency during payment operations

---

#### UC028: Waive Fine

**Use Case ID and Name:** UC028 - Waive Fine

**Author and Date Created:** Technical Lead, June 25, 2025

**Primary Actor:** Librarian, Admin

**Secondary Actors:** Member, System

**Trigger:** Staff determines that a fine should be forgiven due to special circumstances or library policy

**Description:** This use case enables authorized library staff to waive fines when circumstances warrant forgiveness of outstanding charges. The fine status is updated to waived and the member's outstanding balance is reduced without requiring payment.

**Preconditions:**
- PRE-1: Valid fine record must exist in the system
- PRE-2: Fine must be in pending status (not already paid or waived)
- PRE-3: Fine must be linked to a valid member account
- PRE-4: Staff member must have appropriate permissions to waive fines

**Postconditions:**
- POST-1: Fine status is updated to waived
- POST-2: Member's outstanding balance is reduced by the fine amount
- POST-3: Waiver transaction is recorded in the system
- POST-4: Waiver confirmation is provided with reason documentation
- POST-5: All changes are saved permanently to the system

**Normal Flow (28.0):**
1. Staff member accesses fine waiver processing interface
2. System displays pending fines for the member
3. Staff member selects the fine to be waived
4. System validates that the fine exists and is in pending status
5. Staff member provides reason for waiving the fine
6. System confirms waiver details and reason with staff member
7. Staff member confirms the waiver request
8. System updates fine status to waived
9. System reduces member's outstanding balance by fine amount
10. System saves all changes and displays waiver confirmation

**Alternative Flows:**
- 28.1: Multiple Fine Waiver
  - 3a. Staff selects multiple fines for the same member
  - 8a. System processes each fine waiver individually
  - 10a. System provides consolidated waiver confirmation
- 28.2: Bulk Waiver Processing
  - 3a. Staff initiates waiver for multiple members with similar circumstances
  - 8a. System processes each member's fines individually
  - 10a. System provides summary of all waivers processed

**Exceptions:**
- 28.0.E1: Fine Not Found
  - 4a. System cannot locate fine with provided information
  - 4b. System displays fine not found message
  - 4c. Staff member returns to fine selection step
- 28.0.E2: Fine Already Processed
  - 4a. Fine has already been paid or waived
  - 4b. System displays fine status message
  - 4c. Staff member selects different fine or confirms status
- 28.0.E3: Insufficient Permissions
  - 5a. Staff member lacks authorization to waive fines
  - 5b. System displays permission denied message
  - 5c. Staff member must contact supervisor or use appropriate account
- 28.0.E4: System Error
  - 9a. System encounters error during waiver processing
  - 9b. System displays error message and rolls back changes
  - 9c. Staff member can retry waiver or contact administrator

**Priority:** Medium

**Frequency of Use:** 20-80 times per week

**Business Rules:** BR-01, BR-22

**Other Information:**
- Only pending fines can be waived through this process
- Waiver reasons are required and documented for audit purposes
- All waiver transactions are logged with staff member identification
- Member's outstanding balance is updated immediately
- Waived fines remain in system for historical tracking

**Assumptions:**
- Staff have proper authorization levels for fine waiver
- Waiver documentation requirements are clearly defined
- Member balance calculations are accurate and current
- System maintains data consistency during waiver operations

---

#### UC029: View Fine History

**Use Case ID and Name:** UC029 - View Fine History

**Author and Date Created:** Technical Lead, June 25, 2025

**Primary Actor:** Member, Librarian, Admin

**Secondary Actors:** System

**Trigger:** A user needs to view current or historical fine information for tracking or management purposes

**Description:** This use case provides access to fine records, enabling members to view their fine history and staff to manage fine information and assist members. Users can view, search, and filter fines based on various criteria.

**Preconditions:**
- PRE-1: User must be authenticated in the system
- PRE-2: User must have appropriate permissions to view fine records
- PRE-3: Members can only view their own fine history
- PRE-4: Staff can view any member's fine history with proper authorization

**Postconditions:**
- POST-1: Fine information is displayed according to user permissions
- POST-2: Current pending fines show status and amounts owed
- POST-3: Historical fines show complete transaction history
- POST-4: User can navigate and search through fine records
- POST-5: Access to fine data is logged for audit purposes

**Normal Flow (29.0):**
1. User accesses the fine management interface
2. System displays appropriate fine view based on user role
3. User selects search criteria or filtering options
4. System retrieves matching fine records
5. System organizes fines by status, date, and amount
6. System displays fine list with descriptions, amounts, dates, and status
7. User can sort fines by date, status, or amount
8. User selects specific fine for detailed information
9. System displays comprehensive fine details including related loan information
10. User can navigate between fines or export information

**Alternative Flows:**
- 29.1: Pending Fines Only
  - 4a. User applies filter to show only pending fines
  - 6a. System displays total outstanding balance
  - 6b. System highlights overdue fines requiring immediate attention
- 29.2: Member-Specific Fine History
  - 3a. Staff searches for fines for a specific member
  - 4a. System retrieves all fines for that member
  - 6a. System displays complete fine history with member information
- 29.3: Advanced Search
  - 3a. User enters search criteria for fine type, amount range, or date range
  - 4a. System searches across all fine records
  - 6a. System displays matching results with highlighted terms
- 29.4: Export Fine History
  - 10a. User selects export option for record keeping
  - 10b. System generates downloadable fine report
  - 10c. User receives confirmation of export completion

**Exceptions:**
- 29.0.E1: No Fines Found
  - 4a. System finds no fine records matching the criteria
  - 4b. System displays no results message
  - 4c. User can adjust search criteria or filters
- 29.0.E2: Access Denied
  - 2a. System detects insufficient permissions
  - 2b. System displays access denied message
  - 2c. User must contact administrator for proper access
- 29.0.E3: Data Retrieval Error
  - 4a. System encounters error during data retrieval
  - 4b. System displays user-friendly error message
  - 4c. User can retry operation or contact support
- 29.0.E4: Large Dataset Warning
  - 4a. System detects request would return excessive records
  - 4b. System suggests more specific search criteria
  - 4c. User can proceed with pagination or refine search

**Priority:** Medium

**Frequency of Use:** 100-400 times per week

**Business Rules:** BR-03, BR-22, BR-24

**Other Information:**
- Search functionality works across fine descriptions and member names
- Outstanding balance is calculated in real-time for pending fines
- Historical data is preserved for audit and compliance purposes
- Export functionality provides data in standard formats
- System maintains user privacy and data access controls

**Assumptions:**
- Users understand fine status meanings and payment implications
- System maintains complete and accurate fine records
- Privacy policies are properly enforced for member data access
---


### Notification Management Use Cases

#### UC030: Create Notification

**Use Case ID and Name:** UC030 - Create Notification

**Author and Date Created:** Technical Lead, June 25, 2025

**Primary Actor:** System, Admin, Librarian

**Secondary Actors:** Member

**Trigger:** System needs to create a notification for overdue loans, reservation availability, or staff manually creates notifications for members

**Description:** This use case enables the creation of notifications in the system for communicating important information to library members. Notifications can be automatically generated by the system or manually created by staff members.

**Preconditions:**
- PRE-1: User must have appropriate permissions to create notifications
- PRE-2: If notification is for a specific member, the member must exist in the system
- PRE-3: Notification content must be valid and complete

**Postconditions:**
- POST-1: New notification record is created with pending status
- POST-2: Notification is assigned a unique identifier
- POST-3: All notification data is saved to the system
- POST-4: Member association is validated if specified

**Normal Flow (30.0):**
1. Staff member or system initiates notification creation process
2. System validates notification content and format
3. System checks if specific member is specified and validates member existence
4. System creates notification record with pending status
5. System assigns creation date and notification type
6. System saves notification to the system
7. System returns confirmation with notification details

**Alternative Flows:**
- 30.1: System-Generated Notification
  - 1a. System automatically creates notification based on predefined triggers
  - 4a. System creates notification without specific member assignment
- 30.2: Bulk Notification Creation
  - 1a. Staff creates notification for multiple members
  - 6a. System creates individual notification records for each member

**Exceptions:**
- 30.0.E1: Invalid Notification Content
  - 2a. Notification content fails validation (subject too long, empty message)
  - 2b. System displays validation error messages
  - 2c. User must provide valid notification content
- 30.0.E2: Member Not Found
  - 3a. System cannot find member with provided information
  - 3b. System displays member not found message
  - 3c. User must verify member information or create system-wide notification
- 30.0.E3: System Error
  - 6a. System encounters error during notification creation
  - 6b. System displays error message
  - 6c. User can retry operation or contact administrator

**Priority:** High

**Frequency of Use:** Hundreds of notifications created daily (mostly automated)

**Business Rules:** BR-20, BR-21, BR-22

**Other Information:**
- Subject limited to 200 characters, message to 500 characters
- Supports multiple notification types: loan reminders, overdue notices, reservation alerts, system maintenance
- System-wide notifications can be created without specific member assignment
- All notifications start with pending status until sent

**Assumptions:**
- Staff provide complete and valid notification content
- Member information is current when creating member-specific notifications
- System maintains accurate notification delivery status

---

#### UC031: Update Notification

**Use Case ID and Name:** UC031 - Update Notification

**Author and Date Created:** Technical Lead, June 25, 2025

**Primary Actor:** System

**Secondary Actors:** Librarian

**Trigger:** System needs to update notification status from pending to sent, or mark as failed after delivery attempts

**Description:** This use case updates the status of existing notifications to track their delivery progress. When notifications are successfully sent, the system automatically records the delivery timestamp.

**Preconditions:**
- PRE-1: Notification must exist in the system
- PRE-2: New status must be valid
- PRE-3: System must have permissions to update notifications

**Postconditions:**
- POST-1: Notification status is updated in the system
- POST-2: Delivery timestamp is set if status becomes sent
- POST-3: Status change is saved permanently
- POST-4: Update activity is logged for audit purposes

**Normal Flow (31.0):**
1. System initiates notification status update process
2. System validates the new status and notification existence
3. System retrieves the notification record from the system
4. System updates the notification status
5. System sets delivery timestamp if status is changed to sent
6. System saves the updated notification information
7. System confirms successful status update

**Alternative Flows:**
- 31.1: Already Sent Notification
  - 5a. Notification was already marked as sent
  - 5b. System preserves original delivery timestamp
  - 7a. System continues with update confirmation

**Exceptions:**
- 31.0.E1: Notification Not Found
  - 3a. System cannot find notification with specified identifier
  - 3b. System displays notification not found message
  - 3c. Process terminates with error status
- 31.0.E2: Invalid Status Value
  - 2a. New status value is not valid
  - 2b. System displays invalid status error
  - 2c. System requires valid status value to proceed
- 31.0.E3: System Error
  - 6a. System encounters error during update operation
  - 6b. System displays error message
  - 6c. System can retry operation or report failure

**Priority:** High

**Frequency of Use:** Every notification created must be updated when sent

**Business Rules:** BR-20, BR-22

**Other Information:**
- Only notification status can be updated through this process
- Delivery timestamp is automatically managed by the system
- Status transitions are validated to ensure proper workflow
- Failed notifications can be retried or marked for investigation

**Assumptions:**
- Notification identifiers are valid when provided
- Status transitions follow established notification workflow
- System maintains accurate delivery tracking information

---

#### UC032: Mark as Read

**Use Case ID and Name:** UC032 - Mark as Read

**Author and Date Created:** Technical Lead, June 25, 2025

**Primary Actor:** Member

**Secondary Actors:** System

**Trigger:** A member wants to mark individual notifications or all notifications as read to manage their notification status

**Description:** This use case allows members to mark notifications as read, either individually or all at once. The system updates the notification status to read while preserving the notification records for historical tracking.

**Preconditions:**
- PRE-1: Member must be authenticated in the system
- PRE-2: Member must have unread notifications in their list
- PRE-3: Member must have permission to manage their notifications

**Postconditions:**
- POST-1: Selected notifications are marked as read in the database
- POST-2: Read status and timestamp are recorded for each notification
- POST-3: Notifications remain in system for historical reference
- POST-4: System confirms successful operation

**Normal Flow (32.0):**
1. Member accesses their notification list
2. Member selects either individual notification(s) or "Mark All as Read" option
3. System validates member has permission to mark selected notifications as read
4. System displays confirmation prompt with count of notifications to be marked
5. Member confirms they want to mark the notifications as read
6. System updates notification status to "read" and records read timestamp
7. System updates member's notification list display to show read status
8. System displays confirmation message with count of notifications marked as read

**Alternative Flows:**
- 32.1: Mark Single Notification
  - 2a. Member selects individual notification to mark as read
  - 4a. System shows confirmation for single notification
  - 6a. System updates only the selected notification status
- 32.2: Mark All Notifications
  - 2a. Member selects "Mark All as Read" option
  - 4a. System shows confirmation with total count of unread notifications
  - 6a. System updates all member's unread notifications to read status
- 32.3: No Unread Notifications
  - 2a. Member selects "Mark All as Read" but has no unread notifications
  - 8a. System displays message that no notifications needed to be marked

**Exceptions:**
- 32.0.E1: Notification Not Found
  - 3a. System cannot find notification with specified identifier
  - 3b. System displays notification not found message
  - 3c. Member returns to notification list
- 32.0.E2: Access Denied
  - 3a. Member does not have permission to mark notifications as read
  - 3b. System displays access denied message
  - 3c. Member is returned to notification list
- 32.0.E3: System Error
  - 6a. System encounters error during notification status update
  - 6b. System displays error message
  - 6c. Member can retry operation or contact support
- 32.0.E4: Partial Failure (Batch Operation)
  - 6a. Some notifications cannot be updated due to system issues
  - 6b. System completes processing of remaining notifications
  - 6c. System reports partial success with details

**Priority:** Medium

**Frequency of Use:** Members regularly mark notifications as read

**Business Rules:** BR-03, BR-20, BR-22

**Other Information:**
- Marking notification as read updates status field rather than deleting record
- Read timestamp is recorded for audit and tracking purposes
- Notifications remain accessible in notification history
- System provides clear visual distinction between read and unread notifications
- Batch operation provides efficiency for members with many notifications

**Assumptions:**
- Members understand difference between read/unread status and deletion
- Historical notification data is valuable for member reference
- Status-based filtering is sufficient for notification management
- System performance handles status updates efficiently for large volumes

---

#### UC033: View Notifications

**Use Case ID and Name:** UC033 - View Notifications

**Author and Date Created:** Technical Lead, June 25, 2025

**Primary Actor:** Member, Librarian, Admin

**Secondary Actors:** System

**Trigger:** A member wants to view their notifications, staff needs to view notifications for a specific member, or system needs notification count for interface display

**Description:** This use case retrieves and displays notifications for a specific member with filtering options. The user can view all notifications or filter to show only unread notifications. The system also provides notification counts for interface elements like badges.

**Preconditions:**
- PRE-1: Member must exist in the system
- PRE-2: User must be authenticated
- PRE-3: Member can only view their own notifications unless staff override

**Postconditions:**
- POST-1: List of member notifications is displayed according to filter
- POST-2: Notifications include complete message content
- POST-3: Member name is displayed for staff reference
- POST-4: Results are ordered by delivery date with newest first
- POST-5: For count requests, appropriate count is returned

**Normal Flow (33.0):**
1. Member accesses notification interface, staff looks up member notifications, or system requests count
2. System validates member exists and user has permission to view notifications
3. System retrieves notifications for the specified member based on filter (all or unread only)
4. System loads associated member information for display
5. System orders notifications by delivery date with newest first
6. For list requests: System formats notification information for display
7. For count requests: System calculates and returns notification count
8. System displays results or returns count based on request type

**Alternative Flows:**
- 33.1: No Notifications Found
  - 3a. System finds no notifications for the specified member
  - 8a. System displays "No notifications found" message
- 33.2: Count Request Only
  - 1a. System component requests notification count only
  - 7a. System returns count without formatting display information
  - 8a. System provides count for interface element updates

**Exceptions:**
- 33.0.E1: Member Not Found
  - 2a. System cannot find member with specified identifier
  - 8a. System displays "Member not found" message
- 33.0.E2: Access Denied
  - 2a. Member attempts to view another member's notification
  - 8a. User is redirected to their own notification list

**Priority:** Medium

**Frequency of Use:** When users need to view their notifications or staff provide support

**Business Rules:** BR-20, BR-22

**Other Information:**
- System displays complete notification content including subject and full message
- Member information includes name and contact details for staff reference
- System maintains access control to protect member privacy

**Assumptions:**
- Users understand difference between notification list and detailed views
- Member information loads correctly for staff reference

---

#### UC034: Send Overdue Notifications

**Use Case ID and Name:** UC034 - Send Overdue Notifications

**Author and Date Created:** Technical Lead, June 25, 2025

**Primary Actor:** System

**Secondary Actors:** Librarian

**Trigger:** Scheduled daily job runs to check for overdue loans and automatically send reminder notifications to affected members

**Description:** This automated use case identifies loans that are past their due date and creates reminder notifications for the members who have overdue books. The system includes loan details and overdue duration in the notification message.

**Preconditions:**
- PRE-1: System must have active loans in the database
- PRE-2: Loan due dates must be properly maintained and current
- PRE-3: Members must have valid contact information
- PRE-4: Notification system must be operational

**Postconditions:**
- POST-1: Notifications are created for all members with overdue loans
- POST-2: Notification status is updated to sent for each created notification
- POST-3: System records successful processing count for monitoring
- POST-4: All notification operations are completed successfully

**Normal Flow (34.0):**
1. System scheduler initiates daily overdue notification process
2. System retrieves all active loans with due dates before current date
3. System includes member and book information for each overdue loan
4. System calculates number of days overdue for each loan
5. System creates notification message including book title and overdue days
6. System creates notification record for each affected member
7. System marks notification as sent and records delivery timestamp
8. System increments success counter for monitoring purposes
9. System completes processing and reports total notifications sent

**Alternative Flows:**
- 34.1: No Overdue Loans Found
  - 2a. System finds no active loans past their due date
  - 9a. System reports "No overdue loans found" for the day

**Exceptions:**
- 34.0.E1: Member Information Missing
  - 3a. Loan has no associated member or incomplete member data
  - 3b. System skips this loan and continues with remaining loans
  - 8a. Skipped loan is not counted in success total
- 34.0.E2: Notification Creation Failed
  - 6a. System fails to create notification for specific member
  - 6b. System logs error but continues processing other loans
  - 8a. Failed notification is not counted in success total
- 34.0.E3: System Processing Error
  - 9a. System encounters error during batch processing
  - 9b. System reports failure with error details
  - 9c. System may need manual intervention or retry

**Priority:** High

**Frequency of Use:** Daily automated execution

**Business Rules:** BR-20, BR-22

**Other Information:**
- System processes loans in batches for efficient performance
- Creates overdue notice type notifications with standardized content
- Includes book title and number of days overdue in message
- Updates notifications to sent status immediately upon creation
- Daily execution ensures timely member communication

**Assumptions:**
- Loan due dates are accurate and properly maintained
- Member contact information is current and valid
- Notification creation and delivery systems are reliable
- Daily processing frequency is sufficient for overdue management

---

#### UC035: Send Availability Notifications

**Use Case ID and Name:** UC035 - Send Availability Notifications

**Author and Date Created:** Technical Lead, June 25, 2025

**Primary Actor:** System

**Secondary Actors:** Member

**Trigger:** Scheduled daily job runs to check for active reservations where requested books have become available

**Description:** This automated use case identifies active reservations where book copies have become available and notifies members that they can now borrow their reserved books. Members have a limited time to collect their reserved books before the reservation expires.

**Preconditions:**
- PRE-1: System must have active reservations in the database
- PRE-2: Book availability status must be current and accurate
- PRE-3: Member contact information must be valid for notifications
- PRE-4: Notification system must be operational

**Postconditions:**
- POST-1: Notifications are created for all reservations with available books
- POST-2: Members receive notification about book availability
- POST-3: System records successful notification delivery
- POST-4: Reservation pickup deadline is communicated to members

**Normal Flow (35.0):**
1. System scheduler initiates daily reservation availability check
2. System retrieves all active reservations ordered by reservation date
3. System checks each reservation to see if requested book has available copies
4. System identifies reservations where books are now available
5. System creates availability notification for each qualifying reservation
6. System includes book title and 3-day pickup deadline in notification message
7. System sends notification to member via their preferred communication method
8. System records successful notification delivery with timestamp
9. System completes processing and reports total availability notifications sent

**Alternative Flows:**
- 35.1: No Active Reservations
  - 2a. System finds no reservations with active status
  - 9a. System reports "No active reservations found" for the day
- 35.2: No Available Books
  - 3a. System finds no available copies for any reserved books
  - 9a. System reports "No book availability notifications needed" for the day

**Exceptions:**
- 35.0.E1: Member Information Missing
  - 2a. Reservation has incomplete or invalid member contact information
  - 2b. System logs issue and continues with remaining reservations
  - 8a. Failed notification is not counted in success total
- 35.0.E2: Book Information Missing
  - 5a. Reserved book information is incomplete or corrupted
  - 5b. System uses generic book reference in notification
  - 6a. System continues processing with available information
- 35.0.E3: Notification Delivery Failed
  - 7a. System fails to deliver notification to member
  - 7b. System logs delivery failure for follow-up
  - 8a. Failed delivery is not counted in success total

**Priority:** High

**Frequency of Use:** Daily automated execution

**Business Rules:** BR-19, BR-20, BR-22

**Other Information:**
- System processes reservations in chronological order (earliest first)
- Members have 3 days to collect available reserved books
- Notifications include book title and pickup deadline information
- System maintains delivery status for monitoring and follow-up
- Failed notifications are logged for manual review

**Assumptions:**
- Reservation status accurately reflects current active reservations
- Book availability status is updated promptly when books are returned
- Members will respond to availability notifications within the deadline
- Daily processing frequency is sufficient for timely member communication

---

#### UC036: View Notification Details

**Use Case ID and Name:** UC036 - View Notification Details

**Author and Date Created:** Technical Lead, June 25, 2025

**Primary Actor:** Member, Librarian, Admin

**Secondary Actors:** System

**Trigger:** A member wants to view complete details of a specific notification or staff needs to review notification content for support purposes

**Description:** This use case retrieves and displays detailed information for a single notification, including the complete message content, delivery information, and associated member details when accessed by staff.

**Preconditions:**
- PRE-1: Notification must exist in the system
- PRE-2: User must be authenticated
- PRE-3: Member can only view their own notifications unless staff override
- PRE-4: Notification must be accessible (not deleted or expired)

**Postconditions:**
- POST-1: Complete notification details are displayed to user
- POST-2: All notification information is formatted for easy reading
- POST-3: Member information is included for staff access
- POST-4: Notification interaction is logged for audit purposes

**Normal Flow (36.0):**
1. User requests detailed view of specific notification
2. System validates user has permission to view the notification
3. System retrieves complete notification information from database
4. System loads associated member information for context
5. System formats notification content for display
6. System presents detailed notification view including subject, message, and delivery date
7. System logs notification access for audit tracking

**Alternative Flows:**
- 36.1: Staff Viewing Member Notification
  - 2a. Staff member accesses notification details for support purposes
  - 4a. System includes complete member contact and account information
  - 6a. System displays enhanced view with member context
- 36.2: System-Generated Notification
  - 4a. Notification has no specific member association
  - 6a. System displays notification as system-wide communication

**Exceptions:**
- 36.0.E1: Notification Not Found
  - 3a. System cannot locate notification with specified identifier
  - 3b. System displays "Notification not found" message
  - 3c. User is redirected to notification list
- 36.0.E2: Access Denied
  - 2a. Member attempts to view another member's notification
  - 2b. System displays access denied message
  - 2c. User is redirected to their own notification list
- 36.0.E3: Notification Unavailable
  - 3a. Notification exists but is no longer accessible (expired or deleted)
  - 3b. System displays unavailable message
  - 3c. User is provided with general notification information if appropriate

**Priority:** Medium

**Frequency of Use:** When users need detailed notification information or staff provide support

**Business Rules:** BR-20, BR-22

**Other Information:**
- System displays complete notification content including subject and full message
- Delivery timestamp shows when notification was sent to member
- Member information includes name and contact details for staff reference
- System maintains access control to protect member privacy
- Notification viewing does not change read/unread status

**Assumptions:**
- Users understand difference between notification list and detailed views
- Member information loads correctly for staff reference
- System maintains complete notification history for detail retrieval

---


### Category Management Use Cases

#### UC037: Create Category

**Use Case ID and Name:** UC037 - Create Category

**Author and Date Created:** Technical Lead, June 25, 2025

**Primary Actor:** Librarian, Admin

**Secondary Actors:** System

**Trigger:** Library staff needs to create new book categories to improve organization and help members find books more easily

**Description:** This use case allows authorized library staff to create new book categories that will be used to organize and classify books in the library system. Categories help members browse books by subject and assist staff in managing the collection.

**Preconditions:**
- PRE-1: Staff member must be authenticated with category management permissions
- PRE-2: Category name must be provided and meet length requirements (1-100 characters)
- PRE-3: Optional description must not exceed 500 characters if provided
- PRE-4: Category name must be unique in the system

**Postconditions:**
- POST-1: New category is created and available for book assignments
- POST-2: Category appears in all category lists and selection interfaces
- POST-3: Staff can immediately assign books to the new category
- POST-4: Category cover image is stored if provided
- POST-5: Success confirmation is displayed to staff member

**Normal Flow (37.0):**
1. Staff member accesses category creation interface
2. System displays category creation form with required and optional fields
3. Staff member enters category name (required) and optional description
4. Staff member optionally uploads a cover image for visual identification
5. System validates that category name is unique and meets requirements
6. System processes cover image upload if provided
7. System creates new category with provided information
8. System saves category to database and makes it available for use
9. System displays success confirmation and redirects to category management
10. Staff member can now assign books to the new category

**Alternative Flows:**
- 37.1: Category Creation Without Cover Image
  - 4a. Staff member leaves cover image field empty
  - 6a. System creates category without cover image
  - 10a. Staff member can add cover image later through category editing
- 37.2: Immediate Book Assignment
  - 9a. Staff member chooses to assign books immediately after creation
  - 10a. System redirects to book assignment interface for new category

**Exceptions:**
- 37.0.E1: Category Name Already Exists
  - 5a. System detects that category name already exists (case-insensitive)
  - 5b. System displays error message "Category with this name already exists"
  - 5c. Staff member must choose a different name to proceed
- 37.0.E2: Invalid Category Information
  - 5a. Category name is too short, too long, or description exceeds limit
  - 5b. System displays specific validation error messages
  - 5c. Staff member must correct information before proceeding
- 37.0.E3: Cover Image Upload Error
  - 6a. System fails to process uploaded cover image
  - 6b. System displays upload error message
  - 6c. Staff member can retry with different image or proceed without image
- 37.0.E4: System Error
  - 8a. System encounters error while saving category
  - 8b. System displays error message and suggests retry
  - 8c. Staff member can attempt to create category again

**Priority:** High

**Frequency of Use:** 50-200 times per month

**Business Rules:** BR-11, BR-22

**Other Information:**
- Category names must be unique across the entire system
- Cover images help members visually identify categories
- Categories are immediately available for book assignment after creation
- System maintains category creation audit information
- Categories support hierarchical organization for future enhancement

**Assumptions:**
- Staff understand the importance of clear, descriptive category names
- Cover images enhance user experience but are not required
- Category creation is a deliberate process requiring staff authorization
- System can handle reasonable volumes of category creation requests

---

#### UC038: Update Category

**Use Case ID and Name:** UC038 - Update Category

**Author and Date Created:** Technical Lead, June 25, 2025

**Primary Actor:** Librarian, Admin

**Secondary Actors:** System

**Trigger:** Library staff needs to modify existing category information to keep categories current and properly organized

**Description:** This use case allows authorized library staff to update category information including name, description, and cover image. The system ensures data integrity by maintaining unique category names and preserving all book associations during updates.

**Preconditions:**
- PRE-1: Category must exist in the system
- PRE-2: Staff member must have category management permissions
- PRE-3: Updated category name must be unique if changed
- PRE-4: Updated description must meet length requirements if provided
- PRE-5: System must support cover image management

**Postconditions:**
- POST-1: Category information is updated with new values
- POST-2: All books assigned to category continue to show updated information
- POST-3: Cover image is replaced if new image provided
- POST-4: Changes are immediately visible throughout the system
- POST-5: Success confirmation is displayed to staff member

**Normal Flow (38.0):**
1. Staff member selects category to edit from category management interface
2. System retrieves current category information and displays edit form
3. System populates form with existing category name, description, and cover image
4. Staff member modifies category name, description, or uploads new cover image
5. System validates that updated information meets requirements
6. System processes cover image upload if new image provided
7. System verifies category name uniqueness if name was changed
8. System updates category information in the database
9. System confirms successful update and refreshes category displays
10. Staff member sees updated category information throughout system

**Alternative Flows:**
- 38.1: Name Change Only
  - 4a. Staff member changes only the category name
  - 7a. System verifies new name is unique
  - 10a. Updated name appears in all category references
- 38.2: Cover Image Replacement
  - 4a. Staff member uploads new cover image
  - 6a. System replaces existing image with new one
  - 10a. New image appears in category displays immediately
- 38.3: Cover Image Removal
  - 4a. Staff member chooses to remove existing cover image
  - 6a. System removes image reference from category
  - 10a. Category displays without cover image

**Exceptions:**
- 38.0.E1: Category Not Found
  - 2a. System cannot locate category with specified identifier
  - 2b. System displays "Category not found" error message
  - 2c. Staff member is redirected to category management interface
- 38.0.E2: Name Already Exists
  - 7a. System detects updated name conflicts with another existing category
  - 7b. System displays "Category name already exists" error message
  - 7c. Staff member must choose different name to proceed
- 38.0.E3: Invalid Category Information
  - 5a. Updated information fails validation (name length, description length)
  - 5b. System displays specific validation error messages
  - 5c. Staff member must correct information before proceeding
- 38.0.E4: Cover Image Upload Error
  - 6a. System fails to process new cover image upload
  - 6b. System displays upload error message
  - 6c. Staff member can retry with different image or proceed without change
- 38.0.E5: System Update Error
  - 8a. System encounters error while saving category changes
  - 8b. System displays error message and suggests retry
  - 8c. Staff member can attempt update again

**Priority:** High

**Frequency of Use:** 100-300 times per month

**Business Rules:** BR-11, BR-22

**Other Information:**
- Category updates do not affect existing book assignments
- Name uniqueness check excludes the current category being updated
- Cover image changes are immediately reflected in all displays
- System maintains audit trail of category changes
- Updates preserve all existing book-category relationships

**Assumptions:**
- Staff make category updates to improve organization and clarity
- Category changes do not require reassignment of associated books
- System handles concurrent updates appropriately
- Image uploads are processed efficiently for good user experience

---

#### UC039: Delete Category

**Use Case ID and Name:** UC039 - Delete Category

**Author and Date Created:** Technical Lead, June 25, 2025

**Primary Actor:** Librarian, Admin

**Secondary Actors:** System

**Trigger:** Library staff needs to remove unused categories to maintain clean organization and eliminate outdated classification options

**Description:** This use case allows authorized library staff to delete categories that are no longer needed. The system ensures data integrity by preventing deletion of categories that have books assigned to them, maintaining the consistency of the library's organization system.

**Preconditions:**
- PRE-1: Category must exist in the system
- PRE-2: Staff member must have category deletion permissions
- PRE-3: Category must not have any books currently assigned to it
- PRE-4: Category must not have any dependent relationships in the system

**Postconditions:**
- POST-1: Category is permanently removed from the system
- POST-2: Category no longer appears in any category lists or selection interfaces
- POST-3: System maintains data integrity with no orphaned references
- POST-4: Staff receives confirmation of successful deletion
- POST-5: Category management interface is updated to reflect removal

**Normal Flow (39.0):**
1. Staff member selects category for deletion from category management interface
2. System retrieves category information and displays deletion confirmation
3. System shows category details including count of assigned books
4. Staff member reviews deletion impact and confirms deletion intent
5. System validates that category has no assigned books
6. System checks for any other system dependencies
7. System removes category from the database
8. System updates all category interfaces to reflect removal
9. System displays success confirmation to staff member
10. Staff member is redirected to updated category management interface

**Alternative Flows:**
- 39.1: Category Review Before Deletion
  - 3a. Staff member reviews category usage and book assignments
  - 4a. Staff member can cancel deletion if category is still needed
  - 10a. Staff member returns to category management without changes

**Exceptions:**
- 39.0.E1: Category Not Found
  - 2a. System cannot locate category with specified identifier
  - 2b. System displays "Category not found" error message
  - 2c. Staff member is redirected to category management interface
- 39.0.E2: Category Has Assigned Books
  - 5a. System detects that category has books currently assigned to it
  - 5b. System displays error "Cannot delete category because it has assigned books"
  - 5c. Staff member must reassign books to other categories before deletion
- 39.0.E3: System Dependencies Exist
  - 6a. System detects other dependencies that prevent deletion
  - 6b. System displays specific dependency information
  - 6c. Staff member must resolve dependencies before deletion can proceed
- 39.0.E4: Deletion Permission Denied
  - 4a. Staff member does not have sufficient permissions for category deletion
  - 4b. System displays access denied message
  - 4c. Staff member must contact administrator for deletion request
- 39.0.E5: System Deletion Error
  - 7a. System encounters error while removing category
  - 7b. System displays error message and suggests retry
  - 7c. Staff member can attempt deletion again or contact support

**Priority:** Medium

**Frequency of Use:** 50-150 times per month

**Business Rules:** BR-11, BR-12, BR-22

**Other Information:**
- Categories with assigned books cannot be deleted to maintain data integrity
- Staff must reassign books to other categories before deletion
- Deletion is permanent and cannot be undone through normal interface
- System maintains referential integrity throughout deletion process
- Category deletion affects dropdown lists and selection interfaces immediately

**Assumptions:**
- Staff understand that category deletion is permanent
- Book reassignment to other categories is completed before deletion attempts
- System maintains complete referential integrity checking
- Category deletion is an infrequent but necessary maintenance activity

---

#### UC040: View Category Details

**Use Case ID and Name:** UC040 - View Category Details

**Author and Date Created:** Technical Lead, June 25, 2025

**Primary Actor:** Guest, Librarian, Admin, Member

**Secondary Actors:** System

**Trigger:** User needs to view detailed information about a specific category including its description, cover image, and list of books assigned to that category

**Description:** This use case allows users to access comprehensive information about a book category, including its description, visual representation, and complete list of books. This helps members discover books within categories of interest and helps staff manage category content.

**Preconditions:**
- PRE-1: Category must exist in the system
- PRE-2: System must be accessible (authenticated users have full access, guests have read-only access)
- PRE-3: System must be able to retrieve category and associated book information
- PRE-4: Category information must be current and accessible

**Postconditions:**
- POST-1: Complete category details are displayed to user
- POST-2: List of books assigned to category is shown
- POST-3: Category metadata including name, description, and cover image are visible
- POST-4: Book count and category statistics are available
- POST-5: User can navigate to individual book details from category view

**Normal Flow (40.0):**
1. User selects specific category to view detailed information
2. System retrieves category information from database
3. System loads list of books currently assigned to the category
4. System formats category details for display including name and description
5. System displays cover image if available for visual identification
6. System presents complete list of books with basic information
7. System shows category statistics such as total book count
8. User views comprehensive category information and can explore assigned books

**Alternative Flows:**
- 40.1: Category Not Found
  - 2a. System cannot locate category with specified identifier
  - 8a. User receives "Category not found" message
  - 8b. User is redirected to category browsing interface
- 40.2: Category with No Books
  - 3a. Category has no books currently assigned to it
  - 6a. System displays "No books currently assigned to this category"
  - 8a. User sees category information but no book listings

**Exceptions:**
- 40.0.E1: Category Access Error
  - 2a. System cannot retrieve category information due to technical issues
  - 2b. System displays error message "Unable to load category details"
  - 2c. User can retry or contact support for assistance
- 40.0.E2: Book List Loading Error
  - 3a. System can load category but cannot retrieve associated books
  - 6a. System displays category details but shows "Book list unavailable"
  - 8a. User sees partial information and can retry or report issue

**Priority:** High

**Frequency of Use:** 200-500 times per week

**Business Rules:** BR-24

**Other Information:**
- Category details include complete book information for member browsing
- Cover images enhance visual category identification
- Book count provides quick overview of category size
- Users can navigate from category view to individual book details
- System maintains consistent category information across all views

**Assumptions:**
- Users find category-based book browsing helpful for discovery
- Category information loads efficiently even with large book lists
- Book-category relationships are properly maintained and current
- Category details provide sufficient information for user decision-making

---

#### UC041: Browse Categories

**Use Case ID and Name:** UC041 - Browse Categories

**Author and Date Created:** Technical Lead, June 25, 2025

**Primary Actor:** Guest, Librarian, Admin, Member, System

**Secondary Actors:** System

**Trigger:** User needs to browse library categories with optional pagination and search functionality, or system needs complete category list for dropdown selections and book assignment interfaces

**Description:** This use case provides users with the ability to browse library categories with flexible display options including search functionality, pagination for large lists, or complete non-paginated lists for selection interfaces. The system adapts the display based on the context and user needs.

**Preconditions:**
- PRE-1: System must be accessible (authenticated users have full access, guests have read-only access)
- PRE-2: System must have category data available for browsing
- PRE-3: Pagination parameters must be valid if specified
- PRE-4: Search terms must be properly formatted if provided

**Postconditions:**
- POST-1: Categories are displayed according to requested format (paginated or complete)
- POST-2: Search filtering is applied if search terms provided
- POST-3: Categories are ordered alphabetically for consistent browsing
- POST-4: Total count and pagination information are shown when appropriate
- POST-5: Book count is displayed for each category

**Normal Flow (41.0):**
1. User accesses category browsing interface or system requests category list
2. System determines display format based on context (paginated view or complete list)
3. System displays categories ordered alphabetically
4. User can optionally enter search terms to filter categories
5. System applies search filter across category names and descriptions
6. For paginated view: System displays results with navigation controls
7. For complete list: System displays all categories without pagination
8. System shows total number of categories and current page information (if paginated)
9. User can navigate between pages or click on categories for detailed information

**Alternative Flows:**
- 41.1: Paginated Browse with Search
  - 2a. User accesses main category browsing interface
  - 4a. User enters search terms in the search field
  - 5a. System filters categories matching search criteria
  - 6a. System displays filtered results with pagination controls
  - 9a. System maintains search criteria across page navigation
- 41.2: Complete Category List (No Pagination)
  - 1a. System or dropdown interface requests complete category list
  - 2a. System provides non-paginated view for selection purposes
  - 7a. System displays all categories suitable for dropdown menus
  - 9a. User can select categories for book assignment or other operations
- 41.3: No Search Results
  - 5a. Search criteria match no existing categories
  - 6a. System displays "No categories found matching your search"
  - 6b. System provides option to clear search and view all categories
- 41.4: No Categories Available
  - 3a. System has no categories to display
  - 6a. System displays "No categories available" message
  - 9a. User may have option to create categories (if staff)

**Exceptions:**
- 41.0.E1: System Loading Error
  - 3a. System encounters error loading category data
  - 3b. System displays error message with retry option
  - 9a. User can refresh page or contact support
- 41.0.E2: Invalid Search Parameters
  - 4a. Search terms contain invalid characters or exceed length limits
  - 5a. System displays search parameter error message
  - 5b. User must correct search terms to continue
- 41.0.E3: Large Dataset Performance Warning
  - 7a. Category count exceeds recommended limits for non-paginated display
  - 7b. System may experience performance degradation
  - 9a. System suggests using paginated browsing instead

**Priority:** High

**Frequency of Use:** 300-800 times per week

**Business Rules:** BR-24

**Other Information:**
- Categories are displayed in alphabetical order for consistent browsing
- Search functionality is case-insensitive and supports partial matches
- Page size is configurable but defaults to appropriate number for viewing
- Book count for each category helps users understand category size
- Complete category list is essential for book assignment operations
- Results are optimized for both browsing and selection interface use

**Assumptions:**
- Users prefer alphabetical ordering for category browsing
- Search functionality covers most common category discovery needs
- System performance is acceptable for both paginated and complete list operations
- Category browsing serves both discovery and operational selection purposes

---


### Data Queries and Information Retrieval Use Cases

#### UC042: View Overdue Report

**Use Case ID and Name:** UC042 - View Overdue Report

**Author and Date Created:** Technical Lead, June 25, 2025

**Primary Actor:** Librarian, Admin

**Secondary Actors:** System

**Trigger:** Library staff needs to view current overdue loans for collection management and member follow-up activities

**Description:** This use case allows library staff to access a comprehensive report of all loans that are past their due date. The report provides essential information about overdue items and affected members to facilitate collection efforts and member communication.

**Preconditions:**
- PRE-1: Staff member must have loan viewing permissions
- PRE-2: System must have active loan records available
- PRE-3: System date must be accurate for overdue calculation
- PRE-4: Loan due dates must be properly maintained

**Postconditions:**
- POST-1: Complete list of overdue loans is displayed to staff
- POST-2: Loans are ordered by due date (oldest overdue first)
- POST-3: Member and book information is included for each overdue loan
- POST-4: Report is suitable for immediate collection action
- POST-5: Staff can export or print the report for follow-up activities

**Normal Flow (42.0):**
1. Staff member accesses overdue loans report interface
2. System calculates current date and identifies loans past due date
3. System retrieves all loans that are currently overdue
4. System includes member contact information and book details for each loan
5. System orders loans by due date with oldest overdue items first
6. System displays comprehensive overdue loans report
7. Staff member reviews overdue loans and plans collection activities
8. Staff member can contact members or take other appropriate actions

**Alternative Flows:**
- 42.1: No Overdue Loans
  - 3a. System finds no loans that are currently overdue
  - 6a. System displays "No overdue loans found" message
  - 8a. Staff member receives confirmation that all loans are current

**Exceptions:**
- 42.0.E1: System Access Error
  - 2a. System cannot access loan data due to technical issues
  - 2b. System displays error message "Unable to load overdue loans report"
  - 8a. Staff member can retry operation or contact system support

**Priority:** High

**Frequency of Use:** 50-200 times per week

**Business Rules:** BR-24

**Other Information:**
- Report includes member name, contact information, book title, and days overdue
- Loans are prioritized by overdue duration for efficient collection efforts
- System maintains accurate overdue calculations based on current date
- Report can be exported for offline processing and member communication
- Staff can use report to prioritize collection activities and member outreach

**Assumptions:**
- Loan due dates are accurately maintained in the system
- Member contact information is current and accessible
- Staff can take immediate action based on overdue loan information
- Collection activities are guided by overdue loan report data

---

#### UC043: View Fines Report

**Use Case ID and Name:** UC043 - View Fines Report

**Author and Date Created:** Technical Lead, June 25, 2025

**Primary Actor:** Librarian, Admin

**Secondary Actors:** System

**Trigger:** Library staff needs to view all unpaid fines for collection management and financial tracking activities

**Description:** This use case allows library staff to access a comprehensive report of all outstanding fines that require collection. The report provides essential information about unpaid financial obligations to facilitate collection efforts and member account management.

**Preconditions:**
- PRE-1: Staff member must have fine viewing permissions
- PRE-2: System must have fine records available for reporting
- PRE-3: Fine status information must be current and accurate
- PRE-4: Member account information must be accessible

**Postconditions:**
- POST-1: Complete list of pending fines is displayed to staff
- POST-2: Fines are ordered by date (most recent first)
- POST-3: Member names and contact information are included
- POST-4: Fine amounts and details are clearly presented
- POST-5: Report is suitable for collection activities and financial tracking

**Normal Flow (43.0):**
1. Staff member accesses pending fines report interface
2. System retrieves all fines that have not been paid or waived
3. System includes member information and fine details for each unpaid fine
4. System orders fines by date with most recent fines displayed first
5. System calculates total outstanding fine amounts for summary
6. System displays comprehensive pending fines report
7. Staff member reviews pending fines and plans collection activities
8. Staff member can contact members or process payments as appropriate

**Alternative Flows:**
- 43.1: No Pending Fines
  - 2a. System finds no unpaid fines in the system
  - 6a. System displays "No pending fines found" message
  - 8a. Staff member receives confirmation that all fines are current

**Exceptions:**
- 43.0.E1: System Access Error
  - 2a. System cannot access fine data due to technical issues
  - 2b. System displays error message "Unable to load pending fines report"
  - 8a. Staff member can retry operation or contact system support
- 43.0.E2: Member Information Missing
  - 3a. Fine has incomplete member information
  - 3b. System displays fine with "Unknown Member" notation
  - 8a. Staff member can research member information separately

**Priority:** High

**Frequency of Use:** 100-300 times per week

**Business Rules:** BR-03, BR-22, BR-24

**Other Information:**
- Report includes member name, fine amount, fine date, and reason for fine
- Fines are ordered chronologically for efficient processing
- System maintains accurate fine status tracking
- Report includes summary totals for financial oversight
- Staff can use report to prioritize collection efforts and payment processing

**Assumptions:**
- Fine status is accurately maintained as payments and waivers are processed
- Member information is current and accessible for collection activities
- Staff can take immediate action based on pending fine information
- Collection activities are guided by pending fine report data

---

#### UC044: Check Outstanding Fines

**Use Case ID and Name:** UC044 - Check Outstanding Fines

**Author and Date Created:** Technical Lead, June 25, 2025

**Primary Actor:** Librarian, Admin, Member

**Secondary Actors:** System

**Trigger:** Staff or member needs to check the total outstanding fine amount for a specific member account for payment processing or account management

**Description:** This use case allows staff and members to view the total amount of unpaid fines for a specific member account. This information is essential for payment processing, account status verification, and member communication about outstanding financial obligations.

**Preconditions:**
- PRE-1: Member account must exist in the system
- PRE-2: User must have permission to view member's financial information
- PRE-3: Fine records must be available for calculation
- PRE-4: Fine amounts must be accurate and current

**Postconditions:**
- POST-1: Total outstanding fine amount is calculated and displayed
- POST-2: Only unpaid fines are included in the calculation
- POST-3: Amount is accurate to currency precision for payment processing
- POST-4: Zero balance is shown if no outstanding fines exist
- POST-5: Member receives clear information about their financial obligations

**Normal Flow (44.0):**
1. User requests member outstanding balance information
2. System identifies the member account for balance calculation
3. System retrieves all unpaid fines associated with the member
4. System calculates the total amount of outstanding fines
5. System displays the current outstanding balance to the user
6. User receives accurate financial information for decision making
7. User can proceed with payment processing or account management as needed

**Alternative Flows:**
- 44.1: No Outstanding Fines
  - 3a. System finds no unpaid fines for the specified member
  - 5a. System displays $0.00 as the outstanding balance
  - 6a. User receives confirmation of zero outstanding balance
- 44.2: Member Self-Service
  - 1a. Member checks their own outstanding balance
  - 5a. System displays balance with payment options available
  - 7a. Member can proceed to payment interface if balance exists

**Exceptions:**
- 44.0.E1: Member Not Found
  - 2a. System cannot find member account with specified information
  - 2b. System displays "Member not found" message
  - 7a. User must verify member information and retry
- 44.0.E2: System Access Error
  - 3a. System cannot access fine records due to technical issues
  - 3b. System displays error message "Unable to calculate outstanding balance"
  - 7a. User can retry operation or contact system support

**Priority:** High

**Frequency of Use:** 200-500 times per week

**Business Rules:** BR-03, BR-15, BR-24

**Other Information:**
- Outstanding balance includes only fines with pending payment status
- Balance calculation is accurate to currency precision for financial transactions
- System maintains real-time accuracy as fines are paid or waived
- Balance information is essential for payment processing and account management
- Members can view their own balance for self-service payment

**Assumptions:**
- Fine amounts are stored with appropriate currency precision
- Member account validation is handled appropriately by the system
- Pending payment status accurately represents unpaid fines
- Balance calculation handles all edge cases correctly

---

#### UC045: View Dashboard

**Use Case ID and Name:** UC045 - View Dashboard

**Author and Date Created:** Technical Lead, June 25, 2025

**Primary Actor:** Librarian, Admin

**Secondary Actors:** System

**Trigger:** Library staff needs to view comprehensive system statistics and metrics for operational oversight and decision-making

**Description:** This use case provides library staff with access to a comprehensive dashboard showing key system statistics including loan metrics, member activity, collection utilization, and system performance indicators.

**Preconditions:**
- PRE-1: Staff member must have statistical viewing permissions
- PRE-2: System must have sufficient data for meaningful statistics
- PRE-3: Statistical calculations must be current and accurate
- PRE-4: Dashboard interface must be accessible

**Postconditions:**
- POST-1: Current system statistics are displayed in organized dashboard format
- POST-2: Statistics are calculated from most recent available data
- POST-3: Dashboard provides actionable insights for library management
- POST-4: Statistical summaries are suitable for reporting and analysis
- POST-5: Data export options are available for further analysis

**Normal Flow (45.0):**
1. Staff member accesses system statistics dashboard
2. System calculates current statistics from database
3. System displays key metrics including active loans, member counts, and collection statistics
4. Staff member reviews operational metrics and trends
5. System provides filtering options for date ranges and specific metrics
6. Staff member can drill down into specific statistical categories
7. System displays detailed breakdowns and trend analysis
8. Staff member can export statistics for reporting purposes

**Alternative Flows:**
- 45.1: Filtered Statistics View
  - 5a. Staff member applies date range filters to view historical statistics
  - 6a. System recalculates statistics for selected time period
  - 7a. System displays filtered metrics with comparison to previous periods
- 45.2: Category-Specific Statistics
  - 6a. Staff member selects specific categories or departments for analysis
  - 7a. System displays detailed breakdowns by selected categories
  - 8a. System provides comparative analysis between categories
- 45.3: Export Statistics Data
  - 8a. Staff member selects export option for external analysis
  - 8b. System generates downloadable report in CSV or PDF format
  - 8c. System provides confirmation of successful export

**Exceptions:**
- 45.0.E1: Insufficient Data
  - 2a. System has insufficient data for meaningful statistics
  - 2b. System displays "Insufficient data for statistics" message
  - 8a. Staff member can try different date ranges or wait for more data
- 45.0.E2: Statistics Calculation Error
  - 2a. System encounters error during statistical calculations
  - 2b. System displays "Unable to calculate statistics" message
  - 8a. Staff member can retry or contact system administrator
- 45.0.E3: Dashboard Loading Error
  - 1a. System cannot load dashboard interface
  - 1b. System displays error message with retry option
  - 8a. Staff member can refresh page or contact support
- 45.0.E4: Data Access Permission Error
  - 1a. Staff member lacks permission to view certain statistics
  - 1b. System displays limited dashboard with available data only
  - 8a. Staff member sees partial statistics based on their permission level

**Priority:** Medium

**Frequency of Use:** 20-100 times per month

**Business Rules:** BR-22, BR-24

**Other Information:**
- Performance Requirements: Dashboard must load within 3 seconds
- Data Refresh Rate: Statistics updated every 15 minutes during business hours
- Concurrent Users: Support up to 10 staff members viewing dashboard simultaneously
- Data Retention: Historical statistics maintained for 3 years
- Accessibility: Dashboard must be accessible via mobile devices and tablets
- Integration: Statistics pulled from all library management modules in real-time
- Visualization: Charts and graphs must be interactive and exportable
- Caching: Dashboard data cached for 5 minutes to improve performance

**Assumptions:**
- Staff member has valid authentication credentials and appropriate access rights
- System database contains sufficient transactional data for meaningful statistics
- Network connectivity is stable for real-time data retrieval
- Staff member has basic computer literacy to navigate dashboard interface
- Browser supports modern web standards (HTML5, CSS3, JavaScript)
- System maintains accurate timestamps for all transactions and activities
- Statistical calculations are performed using standardized library metrics
- Dashboard interface follows established UI/UX patterns familiar to staff

---

#### UC046: Generate Reports

**Use Case ID and Name:** UC046 - Generate Reports

**Author and Date Created:** Technical Lead, June 25, 2025

**Primary Actor:** Librarian, Admin

**Secondary Actors:** System

**Trigger:** Library staff needs to generate comprehensive reports for management, regulatory compliance, or operational analysis

**Description:** This use case allows library staff to generate various types of reports including collection reports, member activity reports, financial reports, and operational performance reports for management and compliance purposes.

**Preconditions:**
- PRE-1: Staff member must have report generation permissions
- PRE-2: System must have sufficient data for requested report type
- PRE-3: Report parameters must be valid and complete
- PRE-4: System must have adequate resources for report generation

**Postconditions:**
- POST-1: Requested report is generated with current data
- POST-2: Report format is suitable for intended use
- POST-3: Report data is accurate and complete
- POST-4: Generated report can be exported or printed
- POST-5: Report generation activity is logged for audit purposes

**Normal Flow (46.0):**
1. Staff member accesses report generation interface
2. System displays available report types and parameters
3. Staff member selects report type and configures parameters
4. System validates report parameters and data requirements
5. Staff member confirms report generation request
6. System processes report generation with current data
7. System formats report according to specified requirements
8. System presents completed report for review and download
9. Staff member can export report in various formats

**Alternative Flows:**
- 46.1: Scheduled Report Generation
  - 1a. Staff member sets up automatic report generation on schedule
  - 6a. System processes report generation at specified intervals
  - 9a. System automatically delivers reports to designated recipients
- 46.2: Custom Report Parameters
  - 3a. Staff member configures advanced filtering and grouping options
  - 4a. System validates complex parameter combinations
  - 7a. System generates highly customized report based on specific criteria
- 46.3: Report Template Selection
  - 2a. Staff member chooses from predefined report templates
  - 3a. System pre-fills parameters based on template selection
  - 7a. System applies consistent formatting based on selected template
- 46.4: Multi-Format Export
  - 9a. Staff member selects multiple export formats (PDF, Excel, CSV)
  - 9b. System generates report in all requested formats
  - 9c. System provides download links for each format type

**Exceptions:**
- 46.0.E1: Invalid Report Parameters
  - 4a. System detects invalid or conflicting report parameters
  - 4b. System displays parameter validation errors
  - 4c. Staff member must correct parameters before proceeding
- 46.0.E2: Insufficient Data for Report
  - 6a. System finds insufficient data matching report criteria
  - 6b. System displays "No data available for selected criteria" message
  - 6c. Staff member can adjust parameters or select different date range
- 46.0.E3: Report Generation Timeout
  - 6a. Report generation exceeds maximum processing time
  - 6b. System displays timeout error and suggests narrowing criteria
  - 6c. Staff member can modify parameters to reduce report scope
- 46.0.E4: System Resource Limitation
  - 6a. System lacks sufficient resources for large report generation
  - 6b. System displays resource limitation message
  - 6c. Staff member can schedule report for off-peak hours or reduce scope
- 46.0.E5: Export Format Error
  - 8a. System fails to generate report in requested format
  - 8b. System displays format error and offers alternative formats
  - 8c. Staff member can select different export format or contact support

**Priority:** Medium

**Frequency of Use:** 50-200 times per month

**Business Rules:** BR-22, BR-24

**Other Information:**
- Report Formats: Support for PDF, Excel (.xlsx), CSV, and printed output
- Processing Time: Standard reports generated within 2 minutes, complex reports within 10 minutes
- File Size Limits: Maximum report size of 50MB for web download
- Storage: Generated reports stored for 30 days before automatic deletion
- Email Integration: Reports can be automatically emailed to specified recipients
- Template Library: Pre-built templates for common report types available
- Scheduling: Support for one-time, daily, weekly, and monthly report generation
- Audit Trail: All report generation activities logged with user and timestamp information

**Assumptions:**
- Staff member has appropriate permissions to generate requested report types
- System database contains complete and accurate data for report generation
- Report parameters provided by staff member are within acceptable ranges
- Network bandwidth sufficient for large report downloads
- Staff member has necessary software to view generated reports (PDF reader, Excel, etc.)
- System has adequate storage space for temporary report files
- Email system is configured and operational for report delivery
- Staff member understands report content and can interpret generated data
- System maintains data integrity during report generation process

---

#### UC047: View Audit Logs

**Use Case ID and Name:** UC047 - View Audit Logs

**Author and Date Created:** Technical Lead, June 30, 2025

**Primary Actor:** Admin, Librarian

**Secondary Actors:** System

**Trigger:** Library staff needs to review system audit logs for security monitoring, compliance verification, or investigation purposes

**Description:** This use case allows authorized library staff to access comprehensive audit logs that track all significant system activities including user actions, data modifications, login attempts, and system events. The audit log provides detailed information about who performed what actions, when they occurred, and what data was affected, supporting security monitoring, compliance requirements, and investigation activities.

**Preconditions:**
- PRE-1: Staff member must have audit log viewing permissions
- PRE-2: System must have audit logging enabled and operational
- PRE-3: Audit log records must be available in the system
- PRE-4: User must be authenticated with appropriate security clearance

**Postconditions:**
- POST-1: Audit log entries are displayed according to access permissions and filters
- POST-2: Log entries are ordered chronologically with most recent events first
- POST-3: Complete audit trail information is provided for each logged event
- POST-4: Filtered and search results maintain data integrity and completeness
- POST-5: Access to audit logs is recorded for accountability and compliance

**Normal Flow (47.0):**
1. Staff member accesses audit log viewing interface
2. System displays default view of recent audit log entries
3. Staff member can apply filters for date range, user, action type, or affected entities
4. System retrieves and displays filtered audit log entries
5. Staff member can sort logs by timestamp, user, action type, or entity affected
6. Staff member selects specific log entry for detailed information
7. System displays comprehensive details including before/after values for data changes
8. Staff member can search within logs using keywords or specific criteria
9. Staff member can export filtered audit logs for external analysis or reporting
10. System records the audit log access for accountability purposes

**Alternative Flows:**
- 47.1: Security Event Investigation
  - 3a. Staff member filters for security-related events (failed logins, permission changes)
  - 4a. System displays security audit trail with enhanced detail
  - 7a. System highlights potential security concerns or anomalies
- 47.2: User Activity Tracking
  - 3a. Staff member searches for specific user's activities
  - 4a. System displays chronological activity log for selected user
  - 7a. System shows complete user session and action history
- 47.3: Data Change Audit
  - 3a. Staff member filters for data modification events
  - 4a. System displays data change log with before/after values
  - 7a. System provides detailed information about what data was modified
- 47.4: System Event Monitoring
  - 3a. Staff member views system-level events (backups, maintenance, errors)
  - 4a. System displays technical audit trail for system operations
  - 7a. System provides detailed technical information about system events
- 47.5: Compliance Report Generation
  - 9a. Staff member exports audit logs for compliance reporting
  - 9b. System generates formatted audit report suitable for external review
  - 9c. System includes summary statistics and compliance verification data

**Exceptions:**
- 47.0.E1: No Audit Logs Found
  - 4a. System finds no audit log entries matching specified criteria
  - 4b. System displays "No audit logs found for selected criteria" message
  - 4c. Staff member can adjust search criteria or date range
- 47.0.E2: Access Permission Denied
  - 1a. Staff member lacks sufficient permissions for audit log access
  - 1b. System displays "Insufficient permissions to view audit logs" error
  - 1c. Staff member must contact administrator for appropriate access rights
- 47.0.E3: Audit Log Retrieval Error
  - 4a. System encounters error while retrieving audit log data
  - 4b. System displays "Unable to load audit logs" error message
  - 4c. Staff member can retry operation or contact system administrator
- 47.0.E4: Large Dataset Performance
  - 4a. Requested audit log query would return excessive number of records
  - 4b. System displays warning and suggests more specific search criteria
  - 4c. Staff member must narrow search parameters or use pagination
- 47.0.E5: Export Generation Failure
  - 9a. System fails to generate audit log export due to size or format limitations
  - 9b. System displays export error with suggested alternatives
  - 9c. Staff member can reduce export scope or select different format

**Priority:** High

**Frequency of Use:** 10-50 times per week

**Business Rules:** BR-22, BR-24

**Other Information:**
- Audit Log Retention: Logs maintained for minimum 7 years for compliance requirements
- Log Entry Types: User authentication, data modifications, permission changes, system events
- Data Captured: Timestamp, user ID, action performed, entity affected, before/after values
- Search Capabilities: Full-text search across all log fields with advanced filtering options
- Export Formats: CSV, Excel, PDF formats available for audit trail documentation
- Data Integrity: Audit logs are immutable and protected from unauthorized modification
- Performance: Log queries optimized to handle large datasets without system impact
- Security: Audit log access is itself audited for complete accountability chain
- Compliance: Log format and retention meets regulatory and industry standards

**Assumptions:**
- Audit logging system is properly configured and operational at all times
- Staff members understand the importance of audit trails for security and compliance
- System maintains accurate timestamps for all logged events
- Database storage is sufficient for long-term audit log retention requirements
- Staff members have appropriate training to interpret audit log information
- Network connectivity supports efficient retrieval of large audit datasets
- Audit log viewing interface provides adequate search and filtering capabilities
- System performance remains acceptable when processing large audit log queries
- Backup and recovery procedures include audit log data protection
- Staff member understands audit log content and can interpret logged activities

---

## Business Rules

| ID   | Business Rule               | Business Rule Description                                                                 |
|------|-----------------------------|--------------------------------------------------------------------------------------------|
| BR-01 | User Role Permissions       | Only Admin or Librarian can create, update, and delete user accounts.                     |
| BR-02 | User Roles                  | Each user must have one role: Member, Librarian, or Admin.                                |
| BR-03 | User Information Access     | Users can only view their own information; staff access is role-based.                    |
| BR-04 | User Data Privacy           | Sensitive user data must be protected from unauthorized access.                           |
| BR-05 | Password Security           | Passwords must be stored securely and meet minimum complexity requirements.               |
| BR-06 | Book Management Rights      | Only Librarian or Admin can add, edit, or delete books in the system.                     |
| BR-07 | Book Deletion Restriction   | Books with active loans or reservations cannot be deleted.                                |
| BR-08 | Copy Deletion Restriction   | Book copies with active loans or reservations cannot be deleted.                          |
| BR-09 | Copy Status Rules           | Copy statuses include: Available, On Loan, Reserved, Lost.                                |
| BR-10 | Copy Return Validation      | A copy cannot be marked as Available unless it has been properly returned.                |
| BR-11 | Category Management         | Only Librarian or Admin can create, edit, or delete categories.                           |
| BR-12 | Category Deletion Rules     | Categories with assigned books cannot be deleted.                                         |
| BR-13 | Loan Limit                  | Members can borrow up to 5 books at the same time.                                        |
| BR-14 | Standard Loan Period        | The default loan period for each book is 14 days.                                         |
| BR-15 | Overdue Fine Calculation    | Late returns incur fines calculated per overdue day.                                      |
| BR-16 | Loan Eligibility            | Members with unpaid fines or maxed-out loans cannot borrow more books.                    |
| BR-17 | Reservation Condition       | Reservations are allowed only if no copies are currently available.                       |
| BR-18 | Reservation Limit           | Each member can have up to 3 active reservations.                                         |
| BR-19 | Reservation Notification    | The system notifies the first member in the queue when a copy becomes available.          |
| BR-20 | Notification Requirement    | System sends notifications for overdue books, reservations, and fines.                    |
| BR-21 | Notification Format         | Notifications must include a subject and a clear message.                                 |
| BR-22 | Audit Logging Requirement   | Key actions (e.g., user creation, book loans, returns) must be logged with timestamps.    |
| BR-23 | Member Deletion Restriction | Members cannot be deleted if they have active loans, reservations, or unpaid fines.       |
| BR-24 | Role-Based Access Control   | System functionalities are restricted based on user roles (RBAC).                         |
| BR-25 | Staff Override Restrictions | Staff cannot cancel member reservations without proper authorization.                     |
| BR-26 | Password Reset Rate Limiting | Maximum 3 reset requests per email per hour |
