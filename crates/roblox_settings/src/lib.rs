#[cfg(target_os = "windows")]
use std::env;

#[cfg(target_os = "windows")]
fn pull_env(key: &str) -> String {
  env::var(key).unwrap_or_default()
}

#[cfg(target_os = "windows")]
pub(crate) fn get_roblox_client_settings() -> String {
  pull_env("LOCALAPPDATA");
}

#[cfg(target_os = "linux")]
pub(crate) fn get_roblox_client_settings() -> String {
  "".into()
}

#[cfg(target_os = "macos")]
pub(crate) fn get_roblox_client_settings() -> String {
  "/Applications/Roblox.app/who_knows.json".into()
}

pub fn get_elem() {
  println!("{}", get_roblox_client_settings());
}
