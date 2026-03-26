                                    </div>
                                )}
                                {studioSubTab === 'Video' && profileGallery.filter(c => c.Type === 'VIDEO').length === 0 && (
                                    <div className="col-span-full py-10 lg:py-20 flex flex-col items-center justify-center border border-dashed border-white/5 opacity-20">
                                        <Video size={24} className="mb-4 text-[var(--text-color)]" />
                                        <span className="mono text-[8px] uppercase tracking-[0.2em]">VISUAL_FEED_OFFLINE</span>
                                    </div>
                                )}
                                {(studioSubTab === 'Journal' || studioSubTab === 'All') && (
                                    <div className="col-span-full space-y-6">
                                        {isMe && showJournalForm && (
                                            <div className="bg-black border border-[var(--text-color)]/20 p-6 space-y-4">
                                                <div className="flex justify-between items-center border-b border-[var(--text-color)]/10 pb-4">
                                                    <h3 className="mono text-[10px] font-black text-[var(--text-color)] uppercase tracking-[0.3em]">INIT_NEW_ENTRY</h3>
                                                </div>
                                                <input
                                                    id="journal-title"
                                                    type="text"
                                                    placeholder="ENTRY_TITLE..."
                                                    className="w-full bg-black/40 border border-white/5 p-3 text-[10px] text-white mono outline-none focus:border-[var(--text-color)]/40 transition-all tracking-widest"
                                                />
                                                <textarea
                                                    id="journal-content"
                                                    placeholder="ENCODE_CORE_LOG_DATA..."
                                                    className="w-full bg-black/40 border border-white/5 p-3 text-[10px] text-white/60 mono outline-none focus:border-[var(--text-color)]/40 transition-all min-h-[100px] resize-none tracking-wider leading-relaxed"
                                                />
                                                <div className="flex justify-center pt-2">
                                                    <button
                                                        onClick={async () => {
                                                            const titleInput = document.getElementById('journal-title');
                                                            const contentInput = document.getElementById('journal-content');
                                                            if (!titleInput?.value || !contentInput?.value) return;

                                                            try {
                                                                const API = await import('../services/api').then(mod => mod.default);
                                                                await API.Journal.create({
                                                                    Title: titleInput.value,
                                                                    Content: contentInput.value,
                                                                    IsPosted: true,
                                                                    IsPinned: false
                                                                });
                                                                titleInput.value = '';
                                                                contentInput.value = '';
                                                                const res = await API.Journal.getMyJournal();
                                                                setProfileJournal(res.data || []);
                                                                setShowJournalForm(false);
                                                            } catch (err) {
                                                                console.error("Failed to commit log", err);
                                                            }
                                                        }}
                                                        className="px-10 py-3 bg-[var(--text-color)]/10 border border-[var(--text-color)]/40 text-[var(--text-color)] text-[10px] font-black uppercase tracking-[0.4em] hover:bg-[var(--text-color)] hover:text-black transition-all"
                                                    >
                                                        [ COMMIT_LOG_TO_ARCHIVE ]
                                                    </button>
                                                </div>

                                                <div className="flex justify-center gap-8 pt-6 border-t border-white/5">
                                                    <button
                                                        onClick={() => {
                                                            const t = document.getElementById('journal-title');
                                                            const c = document.getElementById('journal-content');
                                                            if (t) t.value = '';
                                                            if (c) c.value = '';
                                                        }}
                                                        className="text-[9px] font-bold text-red-500/40 hover:text-red-500 uppercase mono flex items-center gap-2 transition-all tracking-widest"
                                                    >
                                                        <Database size={12} /> [ PURGE_BUFFER ]
                                                    </button>
                                                    <button
                                                        onClick={() => {
                                                            const t = document.getElementById('journal-title');
                                                            const c = document.getElementById('journal-content');
                                                            if (t) t.value = '';
                                                            if (c) c.value = '';
                                                            setShowJournalForm(false);
                                                        }}
                                                        className="text-[9px] font-bold text-white/20 hover:text-white/60 uppercase mono flex items-center gap-2 transition-all tracking-widest"
                                                    >
                                                        <X size={12} /> [ EXIT_POST_PROTOCOL ]
                                                    </button>
                                                </div>
                                            </div>
                                        )}

                                        <div>
                                            {/* Journal Carousel Header */}
                                            <div className="flex justify-between items-center mb-4">
                                                <h3 className="mono text-[10px] font-black text-[var(--text-color)]/60 uppercase tracking-[0.3em]">
                                                    JOURNAL_ARCHIVE
                                                </h3>
                                            </div>

                                            <div className="relative group px-6 mb-4">
                                                {profileJournal.length > 0 && (
                                                    <>
                                                        <button
                                                            onClick={() => {
                                                                const el = document.getElementById('journal-carousel');
                                                                if (el) el.scrollBy({ left: -400, behavior: 'smooth' });
                                                            }}
                                                            className="absolute left-0 top-1/2 -translate-y-1/2 z-40 text-[var(--text-color)]/60 hover:text-[var(--text-color)] hover:scale-110 transition-all opacity-100"
                                                        >
                                                            <ChevronLeft size={20} />
                                                        </button>
                                                        <button
                                                            onClick={() => {
                                                                const el = document.getElementById('journal-carousel');
                                                                if (el) el.scrollBy({ left: 400, behavior: 'smooth' });
                                                            }}
                                                            className="absolute right-0 top-1/2 -translate-y-1/2 z-40 text-[var(--text-color)]/60 hover:text-[var(--text-color)] hover:scale-110 transition-all opacity-100"
                                                        >
                                                            <ChevronRight size={20} />
                                                        </button>
                                                    </>
                                                )}
                                            </div>
                                        </div>

                                        <div id="journal-carousel" className="flex gap-4 overflow-x-auto pb-4 custom-scrollbar scroll-smooth snap-x snap-mandatory">
                                            {profileJournal.length > 0 ? (
                                                        profileJournal.sort((a, b) => {
                                                            const aPinned = isTruthy(a.IsPinned || a.isPinned) ? 1 : 0;
                                                            const bPinned = isTruthy(b.IsPinned || b.isPinned) ? 1 : 0;
                                                            if (bPinned !== aPinned) return bPinned - aPinned;

                                                            const aPosted = isTruthy(a.IsPosted || a.isPosted) ? 1 : 0;
                                                            const bPosted = isTruthy(b.IsPosted || b.isPosted) ? 1 : 0;
                                                            if (bPosted !== aPosted) return bPosted - aPosted;

                                                            return new Date(b.CreatedAt || b.createdAt) - new Date(a.CreatedAt || a.createdAt);
                                                        })
                                                            .map((entry, idx) => (
                                                                <motion.div
                                                                    key={entry.Id || idx}
                                                                    initial={{ opacity: 0, y: 20 }}
                                                                    animate={{ opacity: 1, y: 0 }}
                                                                    transition={{ delay: idx * 0.1 }}
                                                                    className={`snap-center shrink-0 w-[400px] p-6 border flex flex-col transition-all ${(entry.IsPosted || entry.isPosted) ? 'border-[var(--text-color)]/40 bg-[var(--text-color)]/5 shadow-[0_0_20px_rgba(var(--text-color-rgb),0.02)]' : 'border-white/5 bg-black'}`}
                                                                >
                                                                    <div className="flex justify-between items-start mb-4 shrink-0">
                                                                        <div className="flex flex-col gap-1">
                                                                            <div className="flex items-center gap-3">
                                                                                {(entry.IsPinned || entry.isPinned) && <Star size={12} className="text-white fill-white" />}
                                                                                <h3 className="text-sm font-bold text-white uppercase tracking-wider">{entry.Title || entry.title || '// UNTITLED_LOG'}</h3>
                                                                            </div>
                                                                            <span className="text-[8px] text-[var(--text-color)] mono">{new Date(entry.CreatedAt || entry.createdAt).toLocaleString()}</span>
                                                                        </div>
                                                                        {isMe && (
                                                                            <div className="flex gap-2">
                                                                                <button
                                                                                    onClick={async () => {
                                                                                        try {
                                                                                            const API = await import('../services/api').then(mod => mod.default);
                                                                                            await API.Journal.togglePin(entry.Id || entry.id);
                                                                                            const isPinnedNow = !isTruthy(entry.IsPinned || entry.isPinned);
                                                                                            setProfileJournal(prev => prev.map(j => (String(j.Id || j.id) === String(entry.Id || entry.id)) ? { ...j, isPinned: isPinnedNow, IsPinned: isPinnedNow } : j));
                                                                                            showNotification(isPinnedNow ? "LOG_LOCKED" : "LOG_RELEASED", `ENTRY_${isPinnedNow ? 'PINNED_TO' : 'RECALLED_FROM'}_MONITOR`, "success");
                                                                                        } catch (err) { console.error(err); }
                                                                                    }}
                                                                                    className={`p-1.5 border backdrop-blur-md transition-all ${isTruthy(entry.IsPinned || entry.isPinned) ? 'bg-white text-black border-white shadow-[0_0_15px_#fff]' : 'bg-black/60 border-white/20 text-white/40 hover:text-white hover:border-white/40'}`}
                                                                                    title="Pin to Monitor"
                                                                                >
                                                                                    <Star size={10} fill={isTruthy(entry.IsPinned || entry.isPinned) ? "currentColor" : "none"} />
                                                                                </button>

                                                                                <button
                                                                                    onClick={async () => {
                                                                                        try {
                                                                                            const API = await import('../services/api').then(mod => mod.default);
                                                                                            await API.Journal.togglePost(entry.Id || entry.id);
                                                                                            const isPostedNow = !isTruthy(entry.IsPosted || entry.isPosted);
                                                                                            setProfileJournal(prev => prev.map(j => (String(j.Id || j.id) === String(entry.Id || entry.id)) ? { ...j, isPosted: isPostedNow, IsPosted: isPostedNow } : j));
                                                                                            showNotification(isPostedNow ? "PINNED_TO_WALL" : "REMOVED_FROM_WALL", `ENTRY_${isPostedNow ? 'ATTACHED_TO' : 'DETACHED_FROM'}_PROFILE_SURFACE`, "success");
                                                                                        } catch (err) { console.error(err); }
                                                                                    }}
                                                                                    className={`p-1.5 border backdrop-blur-md transition-all ${isTruthy(entry.IsPosted || entry.isPosted) ? 'bg-[var(--text-color)] text-black border-[var(--text-color)] shadow-[0_0_15px_rgba(255,0,110,0.5)]' : 'bg-black/60 border-[var(--text-color)]/20 text-[var(--text-color)]/40 hover:text-[var(--text-color)] hover:border-[var(--text-color)]/40'}`}
                                                                                    title="Pin to Wall"
                                                                                >
                                                                                    <Share2 size={10} />
                                                                                </button>
                                                                                <button
                                                                                    onClick={() => {
                                                                                        const link = `${window.location.origin}/profile/${targetUserId || currentUser?.id || currentUser?.Id}?journal=${entry.Id || entry.id}`;
                                                                                        navigator.clipboard.writeText(link);
                                                                                        showNotification("LINK_COPIED", "ARCHIVE_SIGNAL_SECURED", "success");
                                                                                    }}
                                                                                    className="px-3 py-1 border border-white/20 text-white/40 hover:text-white transition-all text-[7px] mono uppercase font-bold"
                                                                                >
                                                                                    [ SHARE_LOG ]
                                                                                </button>
                                                                                <button
                                                                                    onClick={async () => {
                                                                                        if (!window.confirm("DELETE_LOG_PERMANENTLY?")) return;
                                                                                        try {
                                                                                            const API = await import('../services/api').then(mod => mod.default);
                                                                                            await API.Journal.delete(entry.Id);
                                                                                            const res = await API.Journal.getMyJournal();
                                                                                            setProfileJournal(res.data || []);
                                                                                        } catch (err) { console.error(err); }
                                                                                    }}
                                                                                    className="px-3 py-1 border border-white/5 text-white/20 hover:text-red-500 hover:border-red-500/30 transition-all text-[7px] mono"
                                                                                >
                                                                                    [ DELETE ]
                                                                                </button>
                                                                            </div>
                                                                        )}
                                                                    </div>
                                                                    <div className="relative">
                                                                        <p className="text-[9px] text-white/60 leading-relaxed italic tracking-wider line-clamp-3">
                                                                            {entry.Content || entry.content}
                                                                        </p>
                                                                        <button
                                                                            onClick={() => setSelectedContent({ ...entry, type: 'JOURNAL' })}
                                                                            className="mt-2 text-[7px] font-bold text-[var(--text-color)] uppercase tracking-widest hover:underline"
                                                                        >
                                                                            [ EXPAND_SIGNAL_DATA ]
                                                                        </button>
                                                                    </div>
                                                                </motion.div>
                                                            ))
                                                    ) : !isLoadingJournal ? (
                                                        <div className="col-span-full py-10 lg:py-20 flex flex-col items-center justify-center border border-dashed border-white/5 opacity-20">
                                                            <Book size={32} className="mb-4 text-[var(--text-color)]" />
                                                            <span className="mono text-[10px] uppercase tracking-[0.2em]">NO_ARCHIVED_LOGS_FOUND</span>
                                                        </div>
                                                    ) : null}
                                                </div>
                                            </div>
                                        )}
                                    </div>
                                )}
