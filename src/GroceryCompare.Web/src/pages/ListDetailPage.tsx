import { useParams } from 'react-router-dom'

export default function ListDetailPage() {
  const { id } = useParams()
  return <h1>Shopping list {id}</h1>
}
