class MinecraftMonitor {
    constructor() {
        console.debug('Minecraft Monitor - Starting Overviewer custom javascript');

        /** @member {L.Marker[]} */
        this.playerMarkers = [];
        console.log(overviewer.map);
        overviewer.map.on('baselayerchange', (event) => this.handleBaseLayerChange(event.layer.tileSetConfig.world));
    }

    /**
     * Handles removing and/or adding the player markers on the map depending on the current dimension.
     * @param {*} currentWorldName
     */
    handleBaseLayerChange(currentWorldName) {
        console.debug(`Minecraft Monitor - World changed: '${currentWorldName}'`);
        Object.entries(this.playerMarkers).forEach(([key, playerMarker]) => {
            if (playerMarker.options.worldName !== currentWorldName) {
                playerMarker.removeFrom(overviewer.map);
            } else if (playerMarker.options.worldName === currentWorldName) {
                playerMarker.addTo(overviewer.map);
            }
        });
    }

    /**
     * Attempts to find the generated world name in Overviewer.
     * Minecraft Monitor stores dimensions like this: `minecraft:overworld`.
     * Overviewer seems to set the path of the tileset to `overworld`.
     * @param {string} dimension
     * @returns {(string|null)}
     */
    getWorldNameFromDimension(dimension) {
        return overviewerConfig?.tilesets?.find((x) => dimension.includes(x.path))?.world;
    }

    async processPlayers(players) {
        console.debug('Minecraft Monitor - Updating map players');
        players
            .sort((a, b) => b.name.localeCompare(a.name)) // Sort by name.
            .sort((a, b) => a.isOnline - b.isOnline) // Sort by online first.
            .forEach((player) => {
                const worldName = this.getWorldNameFromDimension(player.coordinates.dimension);

                // If we can't find the player's dimension it means it's not generated.
                if (worldName === null) {
                    return;
                }

                // Create latLnt.
                const latLnt = overviewer.util.fromWorldToLatLng(
                    player.coordinates.x,
                    player.coordinates.y,
                    player.coordinates.z,
                    overviewer.current_layer[overviewer.current_world].tileSetConfig
                );

                let playerMarker = this.playerMarkers[player.uuid];
                if (playerMarker !== undefined) {
                    // Update existing marker.
                    playerMarker.options.player = player;
                    playerMarker.options.worldName = worldName;
                    playerMarker.setLatLng(latLnt);
                } else {
                    // Create new marker.
                    playerMarker = new L.marker(latLnt, {
                        icon: this.generateIcon(player.uuid),
                        title: player.name,
                        player,
                        worldName: worldName,
                    });

                    // Popup.
                    playerMarker.bindPopup((marker) => this.renderPopup(marker), { minWidth: 400 });

                    // Only add the marker to the map if the current layer is the player's dimension.
                    if (worldName === overviewer.current_world) {
                        playerMarker.addTo(overviewer.map);
                    }

                    this.playerMarkers[player.uuid] = playerMarker;
                }

                // Update marker element with data.
                playerMarker.getElement().setAttribute('data-is-online', player.isOnline);
            });
    }

    /**
     * Renders the Leaflet Popup for a marker.
     * @param {L.Marker} marker
     */
    renderPopup(marker) {
        const player = marker.options.player;

        return `
            <div class="player-popup mud-paper mud-elevation-25 pa-4 d-flex flex-column align-center">
                <div class="player-popup__title mud-typography mud-typography-h6 mud-inherit-text">${player.name}</div>
                <div class="player-popup__info">
                    <div class="player-popup__last-login mud-typography mud-typography-subtitle2 mud-inherit-text">
                        ${
                            player.isOnline
                                ? `<span class="online">Online</span>`
                                : `<span>Last connected: ${new Date(player.lastOnlineDate).toLocaleString()}</span>`
                        }
                    </div>
                </div>                
            </div>
        `;
    }

    /**
     * Creates a `L.Icon` from a `uuid` using `crafatar.com`.
     * @param {string} uuid
     * @returns {L.Icon}
     */
    generateIcon(uuid) {
        const avatarSize = [120, 270];
        const factor = 1 / 4;
        return new L.icon({
            iconUrl: 'https://crafatar.com/renders/body/' + uuid, // 120x270.
            iconSize: [avatarSize[0] * factor, avatarSize[1] * factor],
            iconAnchor: [(avatarSize[0] * factor) / 2, avatarSize[1] * factor],
            popupAnchor: [0, -avatarSize[1] * factor - 5],
            className: 'player-marker',
        });
    }

    resetState() {
        this.playerMarkers = [];
    }
}

window.minecraftMonitor = new MinecraftMonitor();
