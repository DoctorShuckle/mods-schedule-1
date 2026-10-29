# Thin wrapper around scripts/*.py.  Run `make help` for usage.
PYTHON ?= python3
MOD ?=
FLAVOR ?=
VERSION ?=

.DEFAULT_GOAL := help
.PHONY: help build bundle deploy log clean clean-logs

help:
	@echo "make build   [MOD=<mod>] [FLAVOR=mono|il2cpp]   build (default: every mod and flavor)"
	@echo "make bundle  MOD=<mod> FLAVOR=<f> [VERSION=<v>] build and zip to out/bundles/"
	@echo "make deploy  MOD=<mod> FLAVOR=<f>               build and scp the DLL to the game PC"
	@echo "make log                                         fetch and print MelonLoader's Latest.log"
	@echo "make clean                                       remove out/"
	@echo "make clean-logs                                  remove logs/"

build:
	$(PYTHON) scripts/build.py $(if $(MOD),-m $(MOD)) $(if $(FLAVOR),-f $(FLAVOR))

bundle: require-mod-flavor
	$(PYTHON) scripts/bundle.py -m $(MOD) -f $(FLAVOR) $(if $(VERSION),-V $(VERSION))

deploy: require-mod-flavor
	$(PYTHON) scripts/deploy.py -m $(MOD) -f $(FLAVOR)

log:
	$(PYTHON) scripts/log.py

clean:
	rm -rf out

clean-logs:
	rm -rf logs

.PHONY: require-mod-flavor
require-mod-flavor:
	@test -n "$(MOD)" -a -n "$(FLAVOR)" || { echo "usage: make $(MAKECMDGOALS) MOD=<mod> FLAVOR=<mono|il2cpp>" >&2; exit 2; }
