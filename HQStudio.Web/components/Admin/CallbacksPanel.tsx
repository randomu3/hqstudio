'use client'

import { useState, useEffect, useCallback } from 'react'
import { motion, AnimatePresence } from 'framer-motion'
import {
  Phone, Globe, User, Mail, MessageCircle, Users, MoreHorizontal,
  Clock, CheckCircle, XCircle, AlertCircle, Plus, Search,
  Car, Calendar, Trash2, Eye, RefreshCw, LogIn, Inbox
} from 'lucide-react'
import { api, getToken, CallbackRequest, RequestSource, RequestStatus, CallbackStats } from '@/lib/api'

const SOURCE_LABELS: Record<RequestSource, { label: string; icon: React.ReactNode; color: string }> = {
  Website: { label: 'Сайт', icon: <Globe size={12} />, color: 'bg-blue-500/10 text-blue-400 border-blue-500/20' },
  Phone: { label: 'Звонок', icon: <Phone size={12} />, color: 'bg-emerald-500/10 text-emerald-400 border-emerald-500/20' },
  WalkIn: { label: 'Визит', icon: <User size={12} />, color: 'bg-purple-500/10 text-purple-400 border-purple-500/20' },
  Email: { label: 'Почта', icon: <Mail size={12} />, color: 'bg-amber-500/10 text-amber-400 border-amber-500/20' },
  Messenger: { label: 'Мессенджер', icon: <MessageCircle size={12} />, color: 'bg-cyan-500/10 text-cyan-400 border-cyan-500/20' },
  Referral: { label: 'Рекомендация', icon: <Users size={12} />, color: 'bg-pink-500/10 text-pink-400 border-pink-500/20' },
  Other: { label: 'Другое', icon: <MoreHorizontal size={12} />, color: 'bg-neutral-500/10 text-neutral-400 border-neutral-500/20' },
}

const STATUS_LABELS: Record<RequestStatus, { label: string; icon: React.ReactNode; color: string }> = {
  New: { label: 'Новая', icon: <AlertCircle size={12} />, color: 'bg-amber-500/10 text-amber-400 border-amber-500/20' },
  Processing: { label: 'В работе', icon: <Clock size={12} />, color: 'bg-blue-500/10 text-blue-400 border-blue-500/20' },
  Completed: { label: 'Завершена', icon: <CheckCircle size={12} />, color: 'bg-emerald-500/10 text-emerald-400 border-emerald-500/20' },
  Cancelled: { label: 'Отменена', icon: <XCircle size={12} />, color: 'bg-red-500/10 text-red-400 border-red-500/20' },
}

export default function CallbacksPanel() {
  const [callbacks, setCallbacks] = useState<CallbackRequest[]>([])
  const [stats, setStats] = useState<CallbackStats | null>(null)
  const [loading, setLoading] = useState(true)
  const [refreshing, setRefreshing] = useState(false)
  const [filter, setFilter] = useState<{ status?: string; source?: string }>({})
  const [search, setSearch] = useState('')
  const [showAddModal, setShowAddModal] = useState(false)
  const [selectedCallback, setSelectedCallback] = useState<CallbackRequest | null>(null)
  const [authError, setAuthError] = useState(false)

  const loadData = useCallback(async (showRefresh = false) => {
    const token = getToken()
    if (!token) {
      setAuthError(true)
      setLoading(false)
      setRefreshing(false)
      return
    }

    if (showRefresh) setRefreshing(true)
    else setLoading(true)

    setAuthError(false)

    try {
      const [callbacksRes, statsRes] = await Promise.all([
        api.callbacks.getAll(filter),
        api.callbacks.getStats()
      ])

      if (callbacksRes.unauthorized || statsRes.unauthorized) {
        setAuthError(true)
      } else {
        if (callbacksRes.data) setCallbacks(callbacksRes.data)
        if (statsRes.data) setStats(statsRes.data)
      }
    } catch {
      // ignore
    }

    setLoading(false)
    setRefreshing(false)
  }, [filter])

  useEffect(() => {
    const token = getToken()
    if (token) loadData()
  }, [loadData])

  useEffect(() => {
    const handleAuthChange = () => {
      setTimeout(() => {
        const token = getToken()
        if (token) {
          setAuthError(false)
          loadData()
        }
      }, 50)
    }
    window.addEventListener('auth-changed', handleAuthChange)

    const handleStorage = (e: StorageEvent) => {
      if (e.key === 'hq_token' && e.newValue) loadData()
    }
    window.addEventListener('storage', handleStorage)

    return () => {
      window.removeEventListener('auth-changed', handleAuthChange)
      window.removeEventListener('storage', handleStorage)
    }
  }, [loadData])

  useEffect(() => {
    const token = localStorage.getItem('hq_token')
    if (!token) return
    const interval = setInterval(() => loadData(true), 30000)
    return () => clearInterval(interval)
  }, [loadData])

  const handleStatusChange = async (id: number, status: RequestStatus) => {
    await api.callbacks.updateStatus(id, status)
    loadData(true)
  }

  const handleDelete = async (id: number) => {
    if (confirm('Удалить заявку?')) {
      await api.callbacks.delete(id)
      loadData(true)
    }
  }

  const filteredCallbacks = callbacks.filter(c =>
    search === '' ||
    c.name.toLowerCase().includes(search.toLowerCase()) ||
    c.phone.includes(search) ||
    c.carModel?.toLowerCase().includes(search.toLowerCase())
  )

  return (
    <div className="space-y-6 pb-12">
      {/* Header */}
      <div className="flex flex-col md:flex-row justify-between items-start md:items-center gap-4">
        <div className="flex items-center gap-4">
          <h2 className="text-xl font-black uppercase text-white">Заявки на обратный звонок</h2>
          <button
            onClick={() => loadData(true)}
            disabled={refreshing}
            className="p-2 text-neutral-500 hover:text-white hover:bg-white/5 rounded-full transition-all disabled:opacity-50"
            title="Обновить"
          >
            <RefreshCw size={16} className={refreshing ? 'animate-spin' : ''} />
          </button>
        </div>
        <button
          onClick={() => setShowAddModal(true)}
          className="bg-white text-black px-6 py-2 rounded-full text-[10px] font-bold uppercase tracking-widest hover:bg-neutral-200 shadow-xl transition-all"
        >
          + Новая заявка
        </button>
      </div>

      {/* Stats */}
      {stats && (
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
          <StatCard label="Новые" value={stats.totalNew} color="amber" icon={<AlertCircle size={18} />} />
          <StatCard label="В работе" value={stats.totalProcessing} color="blue" icon={<Clock size={18} />} />
          <StatCard label="Сегодня" value={stats.todayCount} color="emerald" icon={<Calendar size={18} />} />
          <StatCard label="За месяц" value={stats.monthCount} color="purple" icon={<Inbox size={18} />} />
        </div>
      )}

      {/* Filters */}
      <div className="bg-neutral-900/40 p-5 rounded-3xl border border-white/5">
        <div className="flex flex-wrap gap-4">
          <div className="relative flex-1 min-w-[200px]">
            <Search size={14} className="absolute left-4 top-1/2 -translate-y-1/2 text-neutral-600" />
            <input
              type="text"
              placeholder="Поиск по имени, телефону, авто..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              className="w-full bg-black border border-white/10 pl-11 pr-4 py-3 rounded-2xl text-white text-sm outline-none focus:border-white/30 transition-all"
            />
          </div>

          <select
            value={filter.status || ''}
            onChange={(e) => setFilter({ ...filter, status: e.target.value || undefined })}
            className="bg-black border border-white/10 px-4 py-3 rounded-2xl text-sm outline-none focus:border-white/30 transition-all min-w-[140px]"
          >
            <option value="">Все статусы</option>
            <option value="New">Новые</option>
            <option value="Processing">В работе</option>
            <option value="Completed">Завершённые</option>
            <option value="Cancelled">Отменённые</option>
          </select>

          <select
            value={filter.source || ''}
            onChange={(e) => setFilter({ ...filter, source: e.target.value || undefined })}
            className="bg-black border border-white/10 px-4 py-3 rounded-2xl text-sm outline-none focus:border-white/30 transition-all min-w-[160px]"
          >
            <option value="">Все источники</option>
            <option value="Website">Сайт</option>
            <option value="Phone">Звонок</option>
            <option value="WalkIn">Визит</option>
            <option value="Email">Почта</option>
            <option value="Messenger">Мессенджер</option>
            <option value="Referral">Рекомендация</option>
            <option value="Other">Другое</option>
          </select>
        </div>
      </div>

      {/* List */}
      <div className="space-y-3">
        {authError ? (
          <div className="bg-neutral-900/40 rounded-3xl border border-white/5 p-16 text-center">
            <LogIn size={48} className="mx-auto text-neutral-700 mb-4" />
            <p className="text-neutral-600 uppercase text-xs tracking-widest italic mb-6">Требуется авторизация</p>
            <button
              onClick={() => loadData()}
              className="bg-white text-black px-6 py-2 rounded-full text-[10px] font-bold uppercase tracking-widest hover:bg-neutral-200 transition-all"
            >
              Повторить
            </button>
          </div>
        ) : loading ? (
          <div className="bg-neutral-900/40 rounded-3xl border border-white/5 p-24 text-center">
            <p className="text-neutral-600 uppercase text-xs tracking-widest italic">Загрузка...</p>
          </div>
        ) : filteredCallbacks.length === 0 ? (
          <div className="bg-neutral-900/40 rounded-3xl border border-white/5 p-24 text-center">
            <p className="text-neutral-600 uppercase text-xs tracking-widest italic">Заявок не найдено</p>
          </div>
        ) : (
          filteredCallbacks.map((callback) => (
            <CallbackCard
              key={callback.id}
              callback={callback}
              onStatusChange={handleStatusChange}
              onDelete={handleDelete}
              onView={() => setSelectedCallback(callback)}
            />
          ))
        )}
      </div>

      {/* Modals */}
      <AnimatePresence>
        {showAddModal && (
          <AddCallbackModal
            onClose={() => setShowAddModal(false)}
            onSave={() => { setShowAddModal(false); loadData(true) }}
          />
        )}
        {selectedCallback && (
          <CallbackDetailModal
            callback={selectedCallback}
            onClose={() => setSelectedCallback(null)}
            onUpdate={() => { setSelectedCallback(null); loadData(true) }}
          />
        )}
      </AnimatePresence>
    </div>
  )
}


function StatCard({ label, value, color, icon }: { label: string; value: number; color: string; icon: React.ReactNode }) {
  const colors: Record<string, string> = {
    amber: 'bg-amber-500/10 border-amber-500/20 text-amber-400',
    blue: 'bg-blue-500/10 border-blue-500/20 text-blue-400',
    emerald: 'bg-emerald-500/10 border-emerald-500/20 text-emerald-400',
    purple: 'bg-purple-500/10 border-purple-500/20 text-purple-400',
  }
  return (
    <div className={`${colors[color]} border rounded-3xl p-5 transition-all hover:scale-[1.02]`}>
      <div className="flex items-center justify-between mb-2">
        <span className="opacity-60">{icon}</span>
        <span className="text-3xl font-black">{value}</span>
      </div>
      <p className="text-[9px] uppercase tracking-widest font-bold opacity-80">{label}</p>
    </div>
  )
}

function CallbackCard({
  callback,
  onStatusChange,
  onDelete,
  onView
}: {
  callback: CallbackRequest
  onStatusChange: (id: number, status: RequestStatus) => void
  onDelete: (id: number) => void
  onView: () => void
}) {
  const source = SOURCE_LABELS[callback.source]
  const status = STATUS_LABELS[callback.status]

  return (
    <motion.div
      initial={{ opacity: 0, y: 10 }}
      animate={{ opacity: 1, y: 0 }}
      className="bg-neutral-900/40 p-6 rounded-3xl border border-white/5 hover:bg-neutral-900/60 transition-all group"
    >
      <div className="flex items-start justify-between gap-6">
        <div className="flex-1 min-w-0">
          <div className="flex items-center gap-3 mb-2">
            <span className="font-bold text-lg text-white truncate">{callback.name}</span>
            <span className={`flex items-center gap-1.5 px-2.5 py-1 rounded-full text-[9px] uppercase tracking-widest font-bold border ${source.color}`}>
              {source.icon} {source.label}
            </span>
          </div>

          <div className="flex flex-wrap items-center gap-4 text-sm text-neutral-400">
            <a href={`tel:${callback.phone}`} className="flex items-center gap-1.5 hover:text-white transition-colors">
              <Phone size={14} /> {callback.phone}
            </a>
            {callback.carModel && (
              <span className="flex items-center gap-1.5">
                <Car size={14} /> {callback.carModel}
              </span>
            )}
          </div>

          {callback.message && (
            <p className="text-xs text-neutral-500 mt-3 line-clamp-2 bg-black/30 p-3 rounded-xl">{callback.message}</p>
          )}

          <div className="flex items-center gap-2 mt-3 text-[9px] text-neutral-600 uppercase tracking-widest">
            <Calendar size={10} />
            {new Date(callback.createdAt).toLocaleString('ru-RU')}
          </div>
        </div>

        <div className="flex flex-col items-end gap-3">
          <span className={`flex items-center gap-1.5 px-3 py-1.5 rounded-xl text-[9px] uppercase tracking-widest font-bold border ${status.color}`}>
            {status.icon} {status.label}
          </span>

          <div className="flex items-center gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
            <button
              onClick={onView}
              className="p-2 text-neutral-500 hover:text-white hover:bg-white/10 rounded-xl transition-all"
              title="Подробнее"
            >
              <Eye size={16} />
            </button>
            {callback.status === 'New' && (
              <button
                onClick={() => onStatusChange(callback.id, 'Processing')}
                className="p-2 text-blue-400 hover:bg-blue-500/20 rounded-xl transition-all"
                title="Взять в работу"
              >
                <Clock size={16} />
              </button>
            )}
            {callback.status === 'Processing' && (
              <button
                onClick={() => onStatusChange(callback.id, 'Completed')}
                className="p-2 text-emerald-400 hover:bg-emerald-500/20 rounded-xl transition-all"
                title="Завершить"
              >
                <CheckCircle size={16} />
              </button>
            )}
            <button
              onClick={() => onDelete(callback.id)}
              className="p-2 text-neutral-600 hover:text-red-500 hover:bg-red-500/10 rounded-xl transition-all"
              title="Удалить"
            >
              <Trash2 size={16} />
            </button>
          </div>
        </div>
      </div>
    </motion.div>
  )
}

function AddCallbackModal({ onClose, onSave }: { onClose: () => void; onSave: () => void }) {
  const [form, setForm] = useState({
    name: '',
    phone: '',
    carModel: '',
    licensePlate: '',
    message: '',
    source: 'WalkIn' as RequestSource,
  })
  const [loading, setLoading] = useState(false)

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!form.name || !form.phone) return

    setLoading(true)
    const result = await api.callbacks.createManual(form)
    setLoading(false)

    if (result.data) onSave()
  }

  return (
    <motion.div
      initial={{ opacity: 0 }}
      animate={{ opacity: 1 }}
      exit={{ opacity: 0 }}
      className="fixed inset-0 bg-black/90 backdrop-blur-xl flex items-center justify-center z-50 p-4"
      onClick={onClose}
    >
      <motion.div
        initial={{ scale: 0.95 }}
        animate={{ scale: 1 }}
        exit={{ scale: 0.95 }}
        className="w-full max-w-md bg-neutral-900 p-8 rounded-[32px] border border-white/10 shadow-2xl"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="text-center mb-8">
          <div className="w-14 h-14 bg-white text-black rounded-2xl flex items-center justify-center mx-auto mb-4 shadow-xl">
            <Phone size={24} />
          </div>
          <h3 className="text-xl font-black uppercase tracking-tighter">Новая заявка</h3>
          <p className="text-[9px] uppercase tracking-widest text-neutral-500 mt-1">Ручное добавление</p>
        </div>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="space-y-1">
            <label className="text-[9px] uppercase tracking-widest text-neutral-500 ml-2 font-black">Источник *</label>
            <select
              value={form.source}
              onChange={(e) => setForm({ ...form, source: e.target.value as RequestSource })}
              className="w-full bg-black border border-white/10 p-4 rounded-2xl text-white outline-none focus:border-white/30 transition-all"
            >
              <option value="WalkIn">Визит в студию</option>
              <option value="Phone">Входящий звонок</option>
              <option value="Email">Электронная почта</option>
              <option value="Messenger">Мессенджер</option>
              <option value="Referral">Рекомендация</option>
              <option value="Other">Другое</option>
            </select>
          </div>

          <div className="space-y-1">
            <label className="text-[9px] uppercase tracking-widest text-neutral-500 ml-2 font-black">Имя клиента *</label>
            <input
              type="text"
              value={form.name}
              onChange={(e) => setForm({ ...form, name: e.target.value })}
              className="w-full bg-black border border-white/10 p-4 rounded-2xl text-white outline-none focus:border-white/30 transition-all"
              placeholder="Иван Иванов"
              required
            />
          </div>

          <div className="space-y-1">
            <label className="text-[9px] uppercase tracking-widest text-neutral-500 ml-2 font-black">Телефон *</label>
            <input
              type="tel"
              value={form.phone}
              onChange={(e) => setForm({ ...form, phone: e.target.value })}
              className="w-full bg-black border border-white/10 p-4 rounded-2xl text-white outline-none focus:border-white/30 transition-all font-mono"
              placeholder="+7 (999) 123-45-67"
              required
            />
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-1">
              <label className="text-[9px] uppercase tracking-widest text-neutral-500 ml-2 font-black">Марка авто</label>
              <input
                type="text"
                value={form.carModel}
                onChange={(e) => setForm({ ...form, carModel: e.target.value })}
                className="w-full bg-black border border-white/10 p-4 rounded-2xl text-white outline-none focus:border-white/30 transition-all"
                placeholder="BMW X5"
              />
            </div>
            <div className="space-y-1">
              <label className="text-[9px] uppercase tracking-widest text-neutral-500 ml-2 font-black">Госномер</label>
              <input
                type="text"
                value={form.licensePlate}
                onChange={(e) => setForm({ ...form, licensePlate: e.target.value.toUpperCase() })}
                className="w-full bg-black border border-white/10 p-4 rounded-2xl text-white outline-none focus:border-white/30 transition-all font-mono"
                placeholder="А123БВ86"
              />
            </div>
          </div>

          <div className="space-y-1">
            <label className="text-[9px] uppercase tracking-widest text-neutral-500 ml-2 font-black">Комментарий</label>
            <textarea
              value={form.message}
              onChange={(e) => setForm({ ...form, message: e.target.value })}
              rows={3}
              className="w-full bg-black border border-white/10 p-4 rounded-2xl text-white outline-none focus:border-white/30 transition-all resize-none"
              placeholder="Интересует шумоизоляция..."
            />
          </div>

          <div className="flex gap-3 pt-4">
            <button
              type="button"
              onClick={onClose}
              className="flex-1 bg-neutral-800 py-4 rounded-2xl text-sm font-bold uppercase tracking-widest hover:bg-neutral-700 transition-all"
            >
              Отмена
            </button>
            <button
              type="submit"
              disabled={loading || !form.name || !form.phone}
              className="flex-1 bg-white text-black py-4 rounded-2xl text-sm font-bold uppercase tracking-widest hover:bg-neutral-200 transition-all disabled:opacity-50 disabled:cursor-not-allowed shadow-xl"
            >
              {loading ? 'Сохранение...' : 'Создать'}
            </button>
          </div>
        </form>
      </motion.div>
    </motion.div>
  )
}


function CallbackDetailModal({
  callback,
  onClose,
  onUpdate
}: {
  callback: CallbackRequest
  onClose: () => void
  onUpdate: () => void
}) {
  const source = SOURCE_LABELS[callback.source]
  const status = STATUS_LABELS[callback.status]

  const handleStatusChange = async (newStatus: RequestStatus) => {
    await api.callbacks.updateStatus(callback.id, newStatus)
    onUpdate()
  }

  return (
    <motion.div
      initial={{ opacity: 0 }}
      animate={{ opacity: 1 }}
      exit={{ opacity: 0 }}
      className="fixed inset-0 bg-black/90 backdrop-blur-xl flex items-center justify-center z-50 p-4"
      onClick={onClose}
    >
      <motion.div
        initial={{ scale: 0.95 }}
        animate={{ scale: 1 }}
        exit={{ scale: 0.95 }}
        className="w-full max-w-lg bg-neutral-900 p-8 rounded-[32px] border border-white/10 shadow-2xl"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="flex items-start justify-between mb-6">
          <div>
            <p className="text-[9px] uppercase tracking-widest text-neutral-500 font-bold mb-1">Заявка #{callback.id}</p>
            <h3 className="text-2xl font-black text-white">{callback.name}</h3>
          </div>
          <span className={`flex items-center gap-1.5 px-3 py-1.5 rounded-xl text-[9px] uppercase tracking-widest font-bold border ${status.color}`}>
            {status.icon} {status.label}
          </span>
        </div>

        <div className="space-y-5">
          <div className="flex items-center gap-2">
            <span className={`flex items-center gap-1.5 px-2.5 py-1 rounded-full text-[9px] uppercase tracking-widest font-bold border ${source.color}`}>
              {source.icon} {source.label}
            </span>
            {callback.sourceDetails && (
              <span className="text-[9px] text-neutral-500 uppercase tracking-widest">({callback.sourceDetails})</span>
            )}
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div className="bg-black/40 p-4 rounded-2xl">
              <p className="text-[9px] uppercase tracking-widest text-neutral-500 font-bold mb-1">Телефон</p>
              <a href={`tel:${callback.phone}`} className="text-lg font-mono text-white hover:text-blue-400 transition-colors">
                {callback.phone}
              </a>
            </div>
            {callback.carModel && (
              <div className="bg-black/40 p-4 rounded-2xl">
                <p className="text-[9px] uppercase tracking-widest text-neutral-500 font-bold mb-1">Автомобиль</p>
                <p className="text-lg text-white">{callback.carModel}</p>
              </div>
            )}
            {callback.licensePlate && (
              <div className="bg-black/40 p-4 rounded-2xl">
                <p className="text-[9px] uppercase tracking-widest text-neutral-500 font-bold mb-1">Госномер</p>
                <p className="text-lg font-mono text-white">{callback.licensePlate}</p>
              </div>
            )}
          </div>

          {callback.message && (
            <div className="bg-black/40 p-4 rounded-2xl">
              <p className="text-[9px] uppercase tracking-widest text-neutral-500 font-bold mb-2">Сообщение</p>
              <p className="text-sm text-neutral-300 leading-relaxed">{callback.message}</p>
            </div>
          )}

          <div className="grid grid-cols-3 gap-3 text-center">
            <div className="bg-black/40 p-3 rounded-2xl">
              <p className="text-[8px] uppercase tracking-widest text-neutral-600 mb-1">Создана</p>
              <p className="text-[10px] font-mono text-white">{new Date(callback.createdAt).toLocaleString('ru-RU')}</p>
            </div>
            {callback.processedAt && (
              <div className="bg-black/40 p-3 rounded-2xl">
                <p className="text-[8px] uppercase tracking-widest text-neutral-600 mb-1">В работе</p>
                <p className="text-[10px] font-mono text-white">{new Date(callback.processedAt).toLocaleString('ru-RU')}</p>
              </div>
            )}
            {callback.completedAt && (
              <div className="bg-black/40 p-3 rounded-2xl">
                <p className="text-[8px] uppercase tracking-widest text-neutral-600 mb-1">Завершена</p>
                <p className="text-[10px] font-mono text-white">{new Date(callback.completedAt).toLocaleString('ru-RU')}</p>
              </div>
            )}
          </div>

          <div className="flex gap-2 pt-4 border-t border-white/5">
            {callback.status === 'New' && (
              <button
                onClick={() => handleStatusChange('Processing')}
                className="flex-1 bg-blue-500/20 text-blue-400 py-3 rounded-2xl text-[10px] font-bold uppercase tracking-widest hover:bg-blue-500/30 transition-all"
              >
                Взять в работу
              </button>
            )}
            {callback.status === 'Processing' && (
              <button
                onClick={() => handleStatusChange('Completed')}
                className="flex-1 bg-emerald-500/20 text-emerald-400 py-3 rounded-2xl text-[10px] font-bold uppercase tracking-widest hover:bg-emerald-500/30 transition-all"
              >
                Завершить
              </button>
            )}
            {(callback.status === 'New' || callback.status === 'Processing') && (
              <button
                onClick={() => handleStatusChange('Cancelled')}
                className="bg-red-500/10 text-red-400 px-5 py-3 rounded-2xl text-[10px] font-bold uppercase tracking-widest hover:bg-red-500/20 transition-all"
              >
                Отменить
              </button>
            )}
            <button
              onClick={onClose}
              className="bg-neutral-800 px-5 py-3 rounded-2xl text-[10px] font-bold uppercase tracking-widest hover:bg-neutral-700 transition-all"
            >
              Закрыть
            </button>
          </div>
        </div>
      </motion.div>
    </motion.div>
  )
}
