namespace APEX.Common.Constraints
{
    /// <summary>
    /// Constraint Connect
    ///     Quadruple particle link
    /// </summary>
    public class ApexConstraintParticleFour
    {
        // particle quadruple
        public int p1, p2, p3, p4;

        public ApexConstraintParticleFour()
        {
            
        }

        public ApexConstraintParticleFour(int p1, int p2, int p3, int p4)
        {
            this.p1 = p1;
            this.p2 = p2;
            this.p3 = p3;
            this.p4 = p4;
        }
    }
}