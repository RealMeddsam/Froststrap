/*
  Froststrap
  Copyright (c) Froststrap Team

  This file is part of Froststrap and is distributed under the terms of the
  GNU Affero General Public License, version 3 or later.

  SPDX-License-Identifier: AGPL-3.0-or-later

  Description: Nix flake for shipping for Nix-darwin, Nix, NixOS, and modules
               of the Nix ecosystem.
*/

{
  description = "Flake for Froststrap";

  inputs = {
    nixpkgs.url = "github:nixos/nixpkgs/nixos-unstable";
    flake-utils.url = "github:numtide/flake-utils";
    treefmt-nix.url = "github:numtide/treefmt-nix";
    naersk.url = "github:nix-community/naersk";
    rust-overlay.url = "github:oxalica/rust-overlay";
  };

  outputs =
    {
      nixpkgs,
      flake-utils,
      treefmt-nix,
      rust-overlay,
      naersk,
      self,
      ...
    }:
    flake-utils.lib.eachDefaultSystem (
      system:
      let
        pkgs = import nixpkgs {
          inherit system;
          overlays = [
            (import rust-overlay)
          ];
        };

        naersk' = pkgs.callPackage naersk { };

        formatters =
          (treefmt-nix.lib.evalModule pkgs (_: {
            projectRootFile = ".git/config";
            programs = {
              nixfmt.enable = true;
              nixf-diagnose.enable = true;
              taplo.enable = true;
              rustfmt.enable = true;
            };
            settings.formatter = {
              rustfmt = {
                options = [
                  "--config"
                  "condense_wildcard_suffixes=true,tab_spaces=2,imports_layout=vertical"
                  "--style-edition"
                  "2024"
                ];
              };
            };
          })).config.build;

        buildInputs =
          with pkgs;
          [
            bacon
            typos
            rust-bin.nightly.latest.default
            cargo-udeps
            clippy
            pkg-config
            rust-analyzer
          ]
          ++ nixpkgs.lib.optionals pkgs.stdenv.isLinux [
            xorg.libxcb
            xorg.xcbutil
            libxkbcommon
            libxkbcommon_8
          ]
          ++ nixpkgs.lib.optionals pkgs.stdenv.isDarwin [
            apple-sdk_15
          ];

        nativeBuildInputs =
          with pkgs;
          lib.optionals pkgs.stdenv.isLinux [
            pkg-config
            xorg.libxcb
            xorg.xcbutil
            libxkbcommon
            libxkbcommon_8
          ];

        runtimeLibs =
          with pkgs;
          lib.optionals pkgs.stdenv.isLinux [
            expat
            fontconfig
            freetype
            freetype.dev
            libGL
            vulkan-loader
            wayland
            xorg.libXi
            xorg.libX11
            xorg.xcbutil
            xorg.libXrandr
            xorg.libXcursor
            xorg.libxcb
            xorg.xcbutil
            libxkbcommon
          ];

        LD_LIBRARY_PATH = pkgs.lib.makeLibraryPath runtimeLibs;
      in
      {
        devShells.default = pkgs.mkShell {
          meta.license = pkgs.lib.licenses.agpl3Plus;
          inherit nativeBuildInputs buildInputs LD_LIBRARY_PATH;

          shellHook =
            if !pkgs.stdenv.isDarwin then
              ''
                #!/bin/bash
                COMMAND=$(awk -F: -v user=$USER 'user == $1 {print $NF}' /etc/passwd)
                if [ "$COMMAND" != *bash* ]; then
                  $COMMAND
                  exit
                fi
              ''
            else
              ''
                #!/bin/bash
                COMMAND=$(dscl . -read $HOME 'UserShell' | grep --only-matching '/.*')
                if [ "$COMMAND" != *bash* ]; then
                  $COMMAND
                  exit
                fi
              '';
        };

        packages.default = naersk'.buildPackage {
          name = "froststrap";
          src = ./.;

          inherit nativeBuildInputs buildInputs LD_LIBRARY_PATH;
        };

        formatter = formatters.wrapper;
        checks.formatting = formatters.check self;
      }
    );
}
