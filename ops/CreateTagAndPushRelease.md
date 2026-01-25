# Creating Tags and Publishing Releases

## Overview

This guide explains how to create Git tags and publish releases for PlcLab. Tagged releases automatically trigger Docker image publication to GitHub Container Registry.

## Semantic Versioning

PlcLab uses [Semantic Versioning (SemVer 2.0)](https://semver.org/) with the format `vMAJOR.MINOR.PATCH`.

- **MAJOR**: Breaking API/feature changes
- **MINOR**: New features (backward compatible)
- **PATCH**: Bug fixes and patches

Examples: `v1.0.0`, `v1.1.0`, `v1.1.1`, `v2.0.0`

## Creating a Release

### 1. Update Version in Project Files

Update the version in `Directory.Packages.props`:

```xml
<Project>
  <PropertyGroup>
    <Version>1.0.0</Version>
    <InformationalVersion>1.0.0</InformationalVersion>
  </PropertyGroup>
  ...
</Project>
```

### 2. Update CHANGELOG.md

Add new section at the top of [ops/CHANGELOG.md](./CHANGELOG.md):

```markdown
## [1.0.0] - 2026-01-25

### Added
- Initial release of PlcLab
- OPC UA client with browse/subscribe/method support
- Blazor web UI with endpoint switcher, browser, signals viewer, test runner
- In-process mock OPC UA server
- Docker support with reference server integration
- GitHub Actions CI/CD pipeline

### Fixed
- [List any bug fixes]

### Changed
- [List any changes]
```

### 3. Commit Changes

```bash
git add Directory.Packages.props ops/CHANGELOG.md
git commit -m "Release v1.0.0"
```

### 4. Create Git Tag

```bash
git tag v1.0.0
```

Annotated tag (recommended, includes tagger info and message):

```bash
git tag -a v1.0.0 -m "Release version 1.0.0 - Initial release"
```

### 5. Push Tag to Remote

```bash
# Push single tag
git push origin v1.0.0

# Push all tags
git push origin --tags
```

### 6. Create GitHub Release (Optional but Recommended)

Go to: https://github.com/jhobel85/PlcLab-Docker-OPC-Blazor/releases/new

- **Tag version**: `v1.0.0`
- **Release title**: `PlcLab v1.0.0`
- **Description**: Copy from CHANGELOG.md
- **Attach binaries**: Download from CI artifacts if needed
- **Set as pre-release**: Check if beta/RC version
- **Publish release**: Click "Publish release"

## Automated Docker Image Publishing

When a tag is pushed:

1. GitHub Actions workflow triggers
2. Runs full build + test + coverage
3. Builds Docker image with tags:
   - `ghcr.io/jhobel85/plclab-docker-opc-blazor:v1.0.0`
   - `ghcr.io/jhobel85/plclab-docker-opc-blazor:1.0`
   - `ghcr.io/jhobel85/plclab-docker-opc-blazor:latest`
4. Pushes all tags to GitHub Container Registry

### Verifying Image Publication

After tag is pushed, monitor workflow:

```bash
# View workflow runs
gh run list --repo jhobel85/PlcLab-Docker-OPC-Blazor

# View logs of specific run
gh run view <run-id> --log
```

Check published images:

```bash
# List all published images
docker search ghcr.io/jhobel85/plclab-docker-opc-blazor

# Pull specific version
docker pull ghcr.io/jhobel85/plclab-docker-opc-blazor:v1.0.0
```

## Pre-release Versions

For beta/release candidate versions:

```bash
# Release candidate
git tag v1.0.0-rc.1
git push origin v1.0.0-rc.1

# Beta release
git tag v1.0.0-beta.1
git push origin v1.0.0-beta.1

# Alpha release
git tag v1.0.0-alpha.1
git push origin v1.0.0-alpha.1
```

Docker images published:
- `v1.0.0-rc.1`
- `1.0.0-rc.1`
- `rc.1`

## Hotfix Releases

For critical production fixes:

```bash
# Create hotfix branch (if needed)
git checkout -b hotfix/v1.0.1 v1.0.0

# Make fixes, commit, and test
git add .
git commit -m "Fix critical security issue"

# Merge back to main and create tag
git checkout main
git merge --no-ff hotfix/v1.0.1
git tag v1.0.1
git push origin main v1.0.1
```

## Release Checklist

Before creating a tag:

- [ ] Update version in `Directory.Packages.props`
- [ ] Update `ops/CHANGELOG.md`
- [ ] Run full test suite locally: `dotnet test`
- [ ] Build Docker image locally: `docker build -t test .`
- [ ] Review all recent commits: `git log --oneline main`
- [ ] Ensure main branch is clean: `git status`
- [ ] All CI checks pass on main branch
- [ ] Create commit with version bump
- [ ] Create and push tag

## Rollback/Undo Release

If you need to undo a release:

### Delete Local Tag

```bash
git tag -d v1.0.0
```

### Delete Remote Tag

```bash
git push origin :refs/tags/v1.0.0
```

Or using newer syntax:

```bash
git push --delete origin v1.0.0
```

### Unpublish Docker Image (if published)

GHCR doesn't support easy image deletion through CLI, but you can delete through:

1. Go to package page: https://github.com/jhobel85/PlcLab-Docker-OPC-Blazor/pkgs/container/plclab-docker-opc-blazor
2. Click on specific version
3. Click "Delete" button (requires admin permissions)

## Version History Example

```
v2.0.0      - Major release with breaking changes
v1.2.3      - Latest patch release
v1.2.2      - Previous patch
v1.2.1      - Earlier patch
v1.2.0      - Previous minor release
v1.1.0      - Earlier minor
v1.0.0      - Initial release
```

## CI/CD Integration

### GitHub Actions Workflow

The `.github/workflows/build.yml` automatically:

1. **On every push to main**:
   - Builds and tests
   - Tags image as `main-<sha>`, `latest`
   - Pushes to GHCR

2. **On tag push** (e.g., `v1.0.0`):
   - Builds and tests
   - Tags image as `v1.0.0`, `1.0`, `1.0.0`
   - Pushes to GHCR

3. **On PR**:
   - Builds and tests (Docker layer caching only)
   - Does NOT push to registry

### Manual Trigger

To manually trigger a workflow without pushing:

```bash
gh workflow run build.yml --repo jhobel85/PlcLab-Docker-OPC-Blazor
```

## Deployment After Release

### Deploy to Production via Docker Compose

```bash
# Update docker-compose.yml with release version
sed -i 's|image:.*|image: ghcr.io/jhobel85/plclab-docker-opc-blazor:v1.0.0|' docker-compose.yml

# Start services
docker compose up -d
```

### Deploy to Kubernetes

Update Helm values or deployment manifest:

```yaml
image:
  repository: ghcr.io/jhobel85/plclab-docker-opc-blazor
  tag: v1.0.0
```

Then apply:

```bash
kubectl apply -f deployment.yaml
```

## Monitoring Releases

### Subscribe to Release Notifications

On GitHub:
1. Go to repository page
2. Click "Watch" → "Custom" → Check "Releases"
3. Receive email for new releases

### Check Release History

```bash
# List all tags
git tag

# Show specific tag info
git show v1.0.0

# List tags with dates
git tag --list --format='%(refname) %(creatordate)'
```

## References

- [Semantic Versioning](https://semver.org/)
- [GitHub Releases](https://docs.github.com/en/repositories/releasing-projects-on-github/about-releases)
- [Git Tagging](https://git-scm.com/book/en/v2/Git-Basics-Tagging)
- [Container Registry Versioning](https://docs.github.com/en/packages/learn-github-packages/publishing-a-package)
