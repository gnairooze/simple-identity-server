# SimpleIdentityServer.WWW

## Setup

1. check connection string in `appsettings.json` file.
2. in Package Manager Console, make sure project **SimpleIdentityServer.WWW** is selected in the default project drop down list, then run `Update-Database`
3. This implementation uses sendgrid to send emails. add the following system environment keys:
	1. SENDGRID_API_KEY
	2. SENDGRID_FROM_EMAIL
	3. SENDGRID_FROM_NAME
4. set *EmailProvider* in `appsettings.json` to 
	1. *SendGrid* for actual sending emails.
	2. *MailHog* for testing purposes. you should setup your own MailHog server or container.


## Development
1. to add new email provider, 
	1. create a new project and add the following dependencies:
		1. for dot net 8: Microsoft.AspNetCore.Identity.UI 8.0.15. Adding version 9 is not working with dot net 8.
		2. Serilog. it is optional to manage logging within your code.
	2. implement `Microsoft.AspNetCore.Identity.UI.Services.IEmailSender` interface.
