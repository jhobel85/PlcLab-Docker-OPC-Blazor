# Release Checklist

1. Ensure `main` is green in CI.
2. Update `CHANGELOG.md` with highlights since last tag.
3. Bump version in docs and any manifests.
4. Create signed tag:
   ```bash
   git tag -a vX.Y.Z -m "Release vX.Y.Z"
   git push origin vX.Y.Z
   ```
5. Create GitHub Release and attach build artifacts (if any).
6. If using containers: build & push images for the tag (see DockerBuildAndPush.md).
7. Announce internally and update deployment environments.
