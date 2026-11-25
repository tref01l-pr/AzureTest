import { useId, useState } from 'react'
import type { UserSummary } from '@entities/user'
import { MIN_SEARCH_LENGTH, useSearchUsers } from '../model/use-search-users'

interface UserSearchProps {
  onSelect: (user: UserSummary) => void
  excludeIds?: string[]
  error?: string
}

export function UserSearch({ onSelect, excludeIds = [], error }: UserSearchProps) {
  const [term, setTerm] = useState('')
  const [active, setActive] = useState(0)
  const id = useId()
  const listId = `${id}-list`
  const { data = [], isFetching } = useSearchUsers(term)
  const options =
    term.trim().length >= MIN_SEARCH_LENGTH ? data.filter((u) => !excludeIds.includes(u.id)) : []
  const open = options.length > 0

  const choose = (user: UserSummary) => {
    onSelect(user)
    setTerm('')
    setActive(0)
  }

  return (
    <div className="relative flex flex-col gap-1">
      <label htmlFor={id} className="text-sm font-medium text-slate-700">
        Add people by email
      </label>
      <input
        id={id}
        type="search"
        role="combobox"
        autoComplete="off"
        aria-expanded={open}
        aria-controls={listId}
        aria-activedescendant={open ? `${listId}-${active}` : undefined}
        aria-invalid={error ? true : undefined}
        aria-describedby={error ? `${id}-error` : undefined}
        value={term}
        placeholder="name@example.com"
        onChange={(e) => {
          setTerm(e.target.value)
          setActive(0)
        }}
        onKeyDown={(e) => {
          if (!open) return
          if (e.key === 'ArrowDown') setActive((a) => Math.min(a + 1, options.length - 1))
          else if (e.key === 'ArrowUp') setActive((a) => Math.max(a - 1, 0))
          else if (e.key === 'Enter') {
            const user = options[active]
            if (user) choose(user)
          } else return
          e.preventDefault()
        }}
        className="rounded-md border border-slate-300 px-3 py-2 shadow-sm focus:outline-none focus:ring-2 focus:ring-indigo-500"
      />
      {isFetching && <span className="text-xs text-slate-500">Searching…</span>}
      {error && (
        <p id={`${id}-error`} className="text-sm text-red-600">
          {error}
        </p>
      )}
      <ul
        id={listId}
        role="listbox"
        hidden={!open}
        className="absolute top-full z-10 mt-1 w-full overflow-hidden rounded-md border border-slate-200 bg-white shadow-lg"
      >
        {options.map((user, i) => (
          <li
            key={user.id}
            id={`${listId}-${i}`}
            role="option"
            aria-selected={i === active}
            onMouseDown={(e) => {
              e.preventDefault()
              choose(user)
            }}
            className={
              i === active ? 'cursor-pointer bg-indigo-50 px-3 py-2' : 'cursor-pointer px-3 py-2'
            }
          >
            {user.email}
          </li>
        ))}
      </ul>
    </div>
  )
}
