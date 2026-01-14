# How to Release a new version of Editor Guidelines to the VS Marketplace

Writing these steps down because I do them so infrequently I have to figure out how to do it each time.

## Prepare for the Release (Copilot assisted)

Create a new GitHub Issue named "Prepare for next release (2.2.13)" (replace 2.2.13 with the next version number)

- In the description write: "Follow the release preparation instructions to prepare for release 2.2.13" (replace 2.2.13 with the next version number)
- Assign it to Copilot.

See, for example, https://github.com/pharring/EditorGuidelines/issues/145 and the corresponding [PR](https://github.com/pharring/EditorGuidelines/pull/146)

Copilot should:
- Update the version number in Directory.Build.props
- Check the copyright year in Directory.Build.props and LICENSE
- Update CHANGELOG.md

Review the files and merge the PR.

## Wait for CI build to complete

- In GitHub Actions, click on "Actions"
- Find the CI workflow run and click on the latest run.
- Wait for it to complete successfully.

## Test the VSIX files

- On a test machine, install the VSIX files.
- Test basic functionality in Visual Studio.
- If necessary, test on downlevel versions of Visual Studio too.

## Draft the Release in GitHub (Pipeline assisted)

Once the preparation PR is merged and the CI build is successful:
- Run the "Create Draft Release" workflow in GitHub Actions.

This will:
- Extract the latest version number from Directory.Build.props.
- Discover the run id for the latest successful CI build from main.
- Download the published artifacts (ZIP files), and extract the contained VSIX files
- Create a new Draft Release with a name and tag like "2.2.13"
- Attach the two VSIX files to the draft release.

## Update the PAT

- There's a good chance that the Personal Access Token (PAT) used for publishing to the VS Marketplace has expired.
- Go to https://visualstudio.com and sign in with the a44s[redacted] account. You may need to use an incognito/private browsing window.
- Find the a44s[redacted] Azure DevOps organization
- Click the "User Settings" button in the top right corner (to the left of the profile picture)
- Select Personal Access Tokens
- Ensure the Access scope is set to the a44s[redacted] organization and verify that you're using the a44s[redacted] account.
- If you don't see any tokens, change the Status filter to "All Tokens". The expired tokens will be listed.
- Create a new PAT with the following settings:
  - Name: "Publish Editor Guidelines to Visual Studio Marketplace"
  - Organization: a44s[redacted]
  - Expiration: Set it to the maximum (1 year).
  - Scopes: Marketplace (Manage)
- Copy the token to the clipboard.
- Go to GitHub, to the repository's Settings, and then to Secrets and Variables > Actions
- Edit the VS_PAT secret and paste the new token into the Value field.

## Publish the Release

- In GitHub, go to the draft release and click the Edit button
- Click "Generate release notes"
- Double-check everything
- Click "Publish release"
- Source code will automatically be added to the release

**Note:** Publishing the release will automatically run the "Publish to VS Marketplace" workflow.

## Upload to the VS Marketplace manually

If the automatic publishing fails, you can upload the VSIX files manually.
- Go to https://marketplace.visualstudio.com/ and sign in using the a44s[redacted] account.
- Click on "Publish Extensions"
- Find the "Editor Guidelines" extension and click on it. (There are two. The one with "Preview" in the name is the Dev17 version.)
- Click "Edit"
- Upload the appropriate VSIX file.
- Hand edit the marketplace description, if necessary.
- Click "Save and Upload"
- The new version will go live in a few minutes (after verification).
