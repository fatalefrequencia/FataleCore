                                                )}
                                            </AnimatePresence>
                                        </div>
                                    )}
                                </div>

                                {/* Studio Content Header / Carousel for Media Tabs */}
                                {['All', 'Photos', 'Video'].includes(studioSubTab) && (
                                    <div className="mb-4 lg:mb-8 space-y-4">
                                        <div className="flex justify-between items-center">
                                            <h3 className="mono text-[10px] font-black text-[var(--text-color)]/60 uppercase tracking-[0.3em]">
                                                {studioSubTab === 'All' ? 'SIGNAL_GALLERY' : studioSubTab === 'Photos' ? 'VISUAL_ARCHIVE' : 'VIDEO_FEED'}
                                            </h3>
                                        </div>
                                        <div className="relative group px-6 mb-4">
                                            {profileGallery.length > 0 && (
                                                <>
                                                    <button
                                                        onClick={() => {
                                                            const el = document.getElementById('media-carousel');
                                                            if (el) el.scrollBy({ left: -300, behavior: 'smooth' });
                                                        }}
                                                        className="absolute left-0 top-1/2 -translate-y-1/2 z-40 text-[var(--text-color)]/60 hover:text-[var(--text-color)] hover:scale-110 transition-all opacity-100"
                                                    >
                                                        <ChevronLeft size={20} />
                                                    </button>
                                                    <button
                                                        onClick={() => {
                                                            const el = document.getElementById('media-carousel');
                                                            if (el) el.scrollBy({ left: 300, behavior: 'smooth' });
                                                        }}
                                                        className="absolute right-0 top-1/2 -translate-y-1/2 z-40 text-[var(--text-color)]/60 hover:text-[var(--text-color)] hover:scale-110 transition-all opacity-100"
                                                    >
                                                        <ChevronRight size={20} />
                                                    </button>
                                                </>
                                            )}
                                            <div
                                                id="media-carousel"
                                                className="flex gap-4 overflow-x-auto pb-4 custom-scrollbar scroll-smooth"
                                            >
                                                {profileGallery.length > 0 ? (
                                                    profileGallery
                                                        .filter(c => studioSubTab === 'All' || (studioSubTab === 'Photos' && c.Type === 'PHOTO') || (studioSubTab === 'Video' && c.Type === 'VIDEO'))
                                                    .sort((a, b) => {
                                                        const aPinned = isTruthy(a.IsPinned || a.isPinned) ? 1 : 0;
                                                        const bPinned = isTruthy(b.IsPinned || b.isPinned) ? 1 : 0;
                                                        if (bPinned !== aPinned) return bPinned - aPinned;

                                                        const aPosted = isTruthy(a.IsPosted || a.isPosted) ? 1 : 0;
                                                        const bPosted = isTruthy(b.IsPosted || b.isPosted) ? 1 : 0;
                                                        if (bPosted !== aPosted) return bPosted - aPosted;

                                                        return new Date(b.CreatedAt || b.createdAt) - new Date(a.CreatedAt || a.createdAt);
                                                    })
                                                    .map((content) => (
                                                        <motion.div
                                                            key={content.Id || content.id}
                                                            initial={{ opacity: 0, scale: 0.9 }}
                                                            animate={{ opacity: 1, scale: 1 }}
                                                            whileHover={{ scale: 1.02 }}
                                                            className="group relative flex-shrink-0 w-64 aspect-square bg-black border border-white/5 overflow-hidden cursor-pointer"
                                                        >
                                                            {/* Hover Controls */}
                                                            {isMe && (
                                                                <div className="absolute top-2 left-2 z-30 flex gap-2 opacity-0 group-hover:opacity-100 transition-all">
                                                                    <button
                                                                        onClick={async (e) => {
                                                                            e.stopPropagation();
                                                                            try {
                                                                                const API = await import('../services/api').then(mod => mod.default);
                                                                                await API.Studio.togglePin(content.Id || content.id);
                                                                                const isPinnedNow = !isTruthy(content.IsPinned || content.isPinned);
                                                                                setProfileGallery(prev => prev.map(c => (String(c.Id || c.id) === String(content.Id || content.id)) ? { ...c, isPinned: isPinnedNow, IsPinned: isPinnedNow } : c));
                                                                                showNotification(isPinnedNow ? "SIGNAL_LOCKED" : "SIGNAL_RELEASED", `CONTENT_${isPinnedNow ? 'PINNED_TO' : 'RECALLED_FROM'}_MONITOR`, "success");
                                                                            } catch (err) { console.error(err); }
                                                                        }}
                                                                        className={`p-1.5 border backdrop-blur-md transition-all ${isTruthy(content.IsPinned || content.isPinned) ? 'bg-white text-black border-white shadow-[0_0_15px_#fff]' : 'bg-black/60 text-white/40 border-white/10 hover:text-white hover:border-white/40'}`}
                                                                        title="Pin to Monitor"
                                                                    >
                                                                        <Star size={10} fill={isTruthy(content.IsPinned || content.isPinned) ? "currentColor" : "none"} />
                                                                    </button>

                                                                    <button
                                                                        onClick={async (e) => {
                                                                            e.stopPropagation();
                                                                            try {
                                                                                const API = await import('../services/api').then(mod => mod.default);
                                                                                await API.Studio.togglePost(content.Id || content.id);
                                                                                const isPostedNow = !isTruthy(content.IsPosted || content.isPosted);
                                                                                setProfileGallery(prev => prev.map(c => (String(c.Id || c.id) === String(content.Id || content.id)) ? { ...c, isPosted: isPostedNow, IsPosted: isPostedNow } : c));
                                                                                showNotification(isPostedNow ? "SIGNAL_BROADCAST" : "SIGNAL_REDACTED", `CONTENT_${isPostedNow ? 'ADDED_TO' : 'REMOVED_FROM'}_WALL`, "success");
                                                                            } catch (err) { console.error(err); }
                                                                        }}
                                                                        className={`p-1.5 border backdrop-blur-md transition-all ${isTruthy(content.IsPosted || content.isPosted) ? 'bg-[var(--theme-color)] text-black border-[var(--theme-color)] shadow-[0_0_15px_rgba(var(--theme-color-rgb),0.5)]' : 'bg-black/60 text-[var(--text-color)]/40 border-[var(--text-color)]/20 hover:text-[var(--text-color)] hover:border-[var(--text-color)]/40'}`}
                                                                        title="Pin to Wall"
                                                                    >
                                                                        <Share2 size={10} />
                                                                    </button>
                                                                    <button
                                                                        onClick={(e) => {
                                                                            e.stopPropagation();
                                                                            const link = `${window.location.origin}/profile/${targetUserId || currentUser?.id || currentUser?.Id}?content=${content.Id || content.id}`;
                                                                            navigator.clipboard.writeText(link);
                                                                            showNotification("LINK_COPIED", "SIGNAL_ADDRESS_SECURED", "success");
                                                                        }}
                                                                        className="p-1.5 border bg-black/60 border-white/10 text-white/40 hover:text-white backdrop-blur-md transition-all"
                                                                        title="Copy Link"
                                                                    >
                                                                        <Share2 size={10} />
                                                                    </button>
                                                                </div>
                                                            )}

                                                            <div className="absolute inset-0 z-10 bg-gradient-to-t from-black/80 via-transparent to-transparent opacity-0 group-hover:opacity-100 transition-opacity p-3 flex flex-col justify-end"
                                                                onClick={() => setSelectedContent({ ...content, type: content.Type || 'PHOTO' })}>
                                                                <div className="text-[7px] mono font-bold text-[var(--text-color)] tracking-widest uppercase mb-1">
                                                                    {content.Type === 'PHOTO' ? '// VISUAL_DATA' : '// SIGNAL_FEED'}
                                                                </div>
                                                                <div className="text-[8px] mono text-white truncate uppercase">{content.Title}</div>
                                                            </div>
                                                            {content.Type === 'VIDEO' ? (
                                                                <div className="w-full h-full flex flex-col items-center justify-center bg-white/5 space-y-2"
                                                                    onClick={() => setSelectedContent({ ...content, type: content.Type || 'PHOTO' })}>
                                                                    <Video size={16} className="text-[var(--text-color)]/40" />
                                                                    <div className="text-[6px] mono text-white/20 uppercase">DECODING_SIGNAL...</div>
                                                                </div>
                                                            ) : (
                                                                <img
                                                                    src={getMediaUrl(content.Url)}
                                                                    alt={content.Title}
                                                                    className="w-full h-full object-cover opacity-60 group-hover:opacity-100 transition-all duration-500"
                                                                    onClick={() => setSelectedContent({ ...content, type: content.Type || 'PHOTO' })}
                                                                />
                                                            )}
                                                        </motion.div>
                                                    ))
                                                ) : !isLoadingGallery ? (
                                                    <div className="w-full flex items-center justify-center py-10 opacity-20 mono text-[8px] uppercase tracking-widest">
                                                        NO_SIGNALS_ARCHIVED_IN_GALLERY
                                                    </div>
                                                ) : null}
                                            </div>
                                        </div>
                                    )}

                                {studioSubTab === 'Photos' && profileGallery.filter(c => c.Type === 'PHOTO').length === 0 && (
                                    <div className="col-span-full py-10 lg:py-20 flex flex-col items-center justify-center border border-dashed border-white/5 opacity-20">
                                        <Camera size={24} className="mb-4 text-[var(--text-color)]" />
                                        <span className="mono text-[8px] uppercase tracking-[0.2em]">GALLERY_ENCRYPTED_OR_EMPTY</span>
                                    </div>
