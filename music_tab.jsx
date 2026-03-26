                                            >
                                                <MessageSquare size={16} />
                                            </button>
                                        )}
                                    </div>
                                </div>
                                
                                {/* Tab Content */}
                                {activeTab === 'Music' && (
                                    profileTracks.length > 0 ? (
                                        <div className="space-y-2 max-h-[400px] overflow-y-auto custom-scrollbar pr-2">
                                            {profileTracks
                                                .sort((a, b) => {
                                                    const aPosted = isTruthy(a.IsPosted || a.isPosted) ? 1 : 0;
                                                    const bPosted = isTruthy(b.IsPosted || b.isPosted) ? 1 : 0;
                                                    if (bPosted !== aPosted) return bPosted - aPosted;

                                                    const dateA = new Date(a.CreatedAt || a.createdAt || 0).getTime();
                                                    const dateB = new Date(b.CreatedAt || b.createdAt || 0).getTime();
                                                    return dateB - dateA;
                                                })
                                                .map((track, idx) => (
                                                    <motion.div
                                                        key={track.id || `track-${idx}`}
                                                        initial={{ opacity: 0, x: -20 }}
                                                        animate={{ opacity: 1, x: 0 }}
                                                        transition={{ delay: idx * 0.05 }}
                                                        className="flex items-center justify-between p-4 bg-transparent border-b border-white/5 hover:border-[var(--text-color)]/30 transition-all group backdrop-blur-[2px] cursor-pointer"
                                                        onClick={() => onPlayTrack(track)}
                                                    >
                                                        <div className="flex items-center gap-6">
                                                            <div className="w-10">
                                                                <span className="text-[10px] text-[var(--text-color)]/20 font-bold mono">[{String(idx + 1).padStart(2, '0')}]</span>
                                                            </div>
                                                            <div className="w-8 h-8 border border-white/10 bg-black overflow-hidden relative grayscale group-hover:grayscale-0 transition-all">
                                                                {track.cover ? (
                                                                    <img src={track.cover} className="w-full h-full object-cover" />
                                                                ) : (
                                                                    <div className="w-full h-full flex items-center justify-center text-[var(--text-color)]/20"><Music size={14} /></div>
                                                                )}
                                                            </div>
                                                            <div>
                                                                <div className="text-sm font-bold text-[var(--text-color)] uppercase tracking-wider group-hover:text-[var(--text-color)]">{track.title}</div>
                                                                <div className="text-[9px] text-[var(--text-color)]/30 uppercase mt-1">SIG_TYPE: {track.genre || 'CORE'} // {track.playCount || 0} READS</div>
                                                            </div>
                                                        </div>
                                                        <div className="flex items-center gap-4">
                                                            {isMe && (
                                                                <div className="flex gap-2 opacity-0 group-hover:opacity-100 transition-all">
                                                                    <button
                                                                        onClick={async (e) => {
                                                                            e.stopPropagation();
                                                                            try {
                                                                                const API = await import('../services/api').then(mod => mod.default);
                                                                                await API.Tracks.togglePost(track.id || track.Id);
                                                                                const isPostedNow = !isTruthy(track.isPosted || track.IsPosted);
                                                                                // Optimistic Update
                                                                                setProfileTracks(prev => prev.map(t => (String(t.id) === String(track.id)) ? { ...t, isPosted: isPostedNow, IsPosted: isPostedNow } : t));
                                                                                showNotification(isPostedNow ? "SIGNAL_BROADCAST" : "SIGNAL_REDACTED", `TRACK_${isPostedNow ? 'ADDED_TO' : 'REMOVED_FROM'}_WALL`, "success");
                                                                            } catch (err) { console.error(err); }
                                                                        }}
                                                                        className={`p-2 border transition-all ${isTruthy(track.isPosted || track.IsPosted) ? 'bg-white text-black border-white shadow-[0_0_15px_#fff]' : 'border-white/10 text-white/40 hover:text-white hover:border-white/30'}`}
                                                                        title="Pin to Wall"
                                                                    >
                                                                        <Star size={12} fill={isTruthy(track.isPosted || track.IsPosted) ? "currentColor" : "none"} />
                                                                    </button>
                                                                    <button
                                                                        onClick={(e) => {
                                                                            e.stopPropagation();
                                                                            const link = `${window.location.origin}/profile/${targetUserId || currentUser?.id || currentUser?.Id}?track=${track.id || track.Id}`;
                                                                            navigator.clipboard.writeText(link);
                                                                            showNotification("LINK_COPIED", "SIGNAL_ADDRESS_SECURED_TO_CLIPBOARD", "success");
                                                                        }}
                                                                        className="p-2 border border-white/10 text-white/40 hover:text-white hover:border-white/30 transition-all"
                                                                        title="Copy Signal Link"
                                                                    >
                                                                        <Share2 size={12} />
                                                                    </button>
                                                                </div>
                                                            )}
                                                            <TrackActionsDropdown track={track} isOwner={isMe} playlists={currentUserPlaylists} myLikes={myLikes} isLikedInitial={myLikes.some(l => (l.trackId || l.TrackId) === (track.id || track.Id))} onDelete={() => handleDeleteTrack(track)} />
                                                        </div>
                                                    </motion.div>
                                                ))}
                                        </div>
                                    ) : !isLoadingTracks ? (
                                        <div className="h-full flex flex-col items-center justify-center opacity-30 text-[10px] font-mono uppercase text-[var(--text-color)] text-center p-8">
                                            <Database size={24} className="mb-4 opacity-50 block mx-auto" />
                                            NO_SIGNALS_DETECTED_IN_CORE
                                        </div>
                                    ) : null
                                )}

                        {activeTab === 'Playlists' && (
                            <div className="grid grid-cols-2 lg:grid-cols-3 gap-6 max-h-[400px] overflow-y-auto custom-scrollbar pr-2 pb-8">
                                {isMe && (
                                    <button
                                        onClick={() => setShowCreatePlaylist(true)}
                                        className="border border-[var(--text-color)]/10 p-4 hover:border-[var(--text-color)]/40 transition-all cursor-pointer group bg-black/20 flex flex-col items-center justify-center gap-4 text-[var(--text-color)]/20 hover:text-[var(--text-color)]"
                                    >
                                        <Plus size={32} />
