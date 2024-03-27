# PDX Mods Bridge

This enables modders to use standard IMod interfaces of Cities Skylines 2 outside of official mod loader. This means you can for the most part upload mods to Thunderstore with identical runtimes to PDX Mods.

Modders can also remove BepInEx as a dependency as it is only required for this loader.

## Setup

For modders, just use the normal IMod interface from the game. Then add this 'Description' attribute with the exact value:

```
[Description("bridge)]
class TestMod : IMod
{
}
```

Now you can upload it to Thunderstore and it will load automatically without BepInEx as a direct dependency for your mod.

### Prerequisites

- Ensure you have BepInEx 5 installed in your Cities Skylines 2 game directory. If not, follow the [BepInEx 5 installation guide](https://github.com/BepInEx/BepInEx).
- 
## Contributing

Contributions are welcome! If you have suggestions or fixes, please fork this repository, make your changes, and submit a pull request.

## Support

Encounter an issue or have questions? Please open an issue on this GitHub repository.

## License

PDXModsBridge is released under the GNU General Public License v2.0. For more details, see the [LICENSE](LICENSE) file included in this repository.